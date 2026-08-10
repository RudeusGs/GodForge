using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models.Analysis;
using GodForge.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GodForge.Infrastructure.Git;

public sealed class GitWorkspaceService : IRepositoryWorkspaceService
{
    private static readonly Dictionary<Guid, RepositoryLockEntry> RepositoryLocks = new();
    private static readonly object RepositoryLocksSync = new();
    private static readonly TimeSpan InitialLimitMonitorInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaximumLimitMonitorInterval = TimeSpan.FromSeconds(30);
    private const int MaximumCapturedGitOutputCharacters = 64 * 1024;

    internal static int RepositoryLockCount
    {
        get
        {
            lock (RepositoryLocksSync)
                return RepositoryLocks.Count;
        }
    }

    private readonly RepositoryProcessingSettings _settings;
    private readonly IRepositoryLockProvider _repositoryLockProvider;
    private readonly ILogger<GitWorkspaceService> _logger;

    public GitWorkspaceService(
        IOptions<RepositoryProcessingSettings> settings,
        IRepositoryLockProvider repositoryLockProvider,
        ILogger<GitWorkspaceService> logger)
    {
        _settings = settings.Value;
        _repositoryLockProvider = repositoryLockProvider;
        _logger = logger;
    }

    public async Task<WorkspaceSyncResult> SyncAsync(
        Guid repositoryId,
        string remoteUrl,
        string branch,
        CancellationToken cancellationToken = default)
    {
        _ = ParseRemoteUri(remoteUrl);
        ValidateBranch(branch);

        var gate = RentRepositoryLock(repositoryId);
        var lockTaken = false;
        try
        {
            await gate.Semaphore.WaitAsync(cancellationToken);
            lockTaken = true;

            await using var distributedLock = await _repositoryLockProvider.AcquireAsync(
                repositoryId,
                TimeSpan.FromSeconds(_settings.GitCommandTimeoutSeconds),
                cancellationToken);
            return await SyncCoreAsync(repositoryId, remoteUrl, branch, cancellationToken);
        }
        finally
        {
            if (lockTaken)
                gate.Semaphore.Release();
            ReturnRepositoryLock(repositoryId, gate);
        }
    }

    private static RepositoryLockEntry RentRepositoryLock(Guid repositoryId)
    {
        lock (RepositoryLocksSync)
        {
            if (!RepositoryLocks.TryGetValue(repositoryId, out var entry))
            {
                entry = new RepositoryLockEntry();
                RepositoryLocks.Add(repositoryId, entry);
            }

            entry.ReferenceCount++;
            return entry;
        }
    }

    private static void ReturnRepositoryLock(Guid repositoryId, RepositoryLockEntry entry)
    {
        var dispose = false;
        lock (RepositoryLocksSync)
        {
            entry.ReferenceCount--;
            if (entry.ReferenceCount < 0)
                throw new InvalidOperationException("Repository lock reference count became invalid.");

            if (entry.ReferenceCount == 0)
            {
                if (!RepositoryLocks.Remove(repositoryId, out var removed) || !ReferenceEquals(removed, entry))
                    throw new InvalidOperationException("Repository lock registry became inconsistent.");
                dispose = true;
            }
        }

        if (dispose)
            entry.Semaphore.Dispose();
    }

    private async Task<WorkspaceSyncResult> SyncCoreAsync(
        Guid repositoryId,
        string remoteUrl,
        string branch,
        CancellationToken cancellationToken)
    {
        var workspaceRoot = Path.GetFullPath(_settings.WorkspaceRoot);
        Directory.CreateDirectory(workspaceRoot);
        var workspacePath = Path.GetFullPath(Path.Combine(workspaceRoot, repositoryId.ToString("N")));
        EnsureChildPath(workspaceRoot, workspacePath);

        var gitDirectory = Path.Combine(workspacePath, ".git");
        var isNewWorkspace = !Directory.Exists(gitDirectory);
        try
        {
            if (isNewWorkspace)
            {
                if (Directory.Exists(workspacePath))
                    Directory.Delete(workspacePath, recursive: true);

                var remote = await ValidateRemoteUrlAsync(remoteUrl, cancellationToken);
                await RunGitAsync(
                    BuildNetworkArguments(remote,
                        "clone", "--no-tags", "--single-branch", "--depth", "1",
                        "--filter=blob:none", "--no-checkout",
                        "--branch", branch, "--", remoteUrl, workspacePath),
                    cancellationToken,
                    workspacePath);
                await EnsureRemoteTreeFileCountAsync(workspacePath, "HEAD", cancellationToken);
                await RunGitAsync(
                    BuildNetworkArguments(remote,
                        "-C", workspacePath, "checkout", "-B", branch, "HEAD"),
                    cancellationToken,
                    workspacePath);
            }
            else
            {
                EnsureRepositoryLimits(workspacePath, deleteOnViolation: true);
                var remote = await ValidateRemoteUrlAsync(remoteUrl, cancellationToken);
                await RunGitAsync(
                    new[] { "-C", workspacePath, "remote", "set-url", "origin", remoteUrl },
                    cancellationToken);
                await RunGitAsync(
                    BuildNetworkArguments(remote,
                        "-C", workspacePath, "fetch", "--prune", "--depth", "1",
                        "--filter=blob:none", "origin", branch),
                    cancellationToken,
                    workspacePath);
                await EnsureRemoteTreeFileCountAsync(workspacePath, $"origin/{branch}", cancellationToken);
                await RunGitAsync(
                    BuildNetworkArguments(remote,
                        "-C", workspacePath, "checkout", "-B", branch, $"origin/{branch}"),
                    cancellationToken,
                    workspacePath);
                await RunGitAsync(
                    BuildNetworkArguments(remote,
                        "-C", workspacePath, "reset", "--hard", $"origin/{branch}"),
                    cancellationToken,
                    workspacePath);
                await RunGitAsync(
                    new[] { "-C", workspacePath, "clean", "-ffd" },
                    cancellationToken);
            }

            var commitSha = (await RunGitAsync(
                new[] { "-C", workspacePath, "rev-parse", "HEAD" },
                cancellationToken)).Trim();
            if (commitSha.Length != 40 || commitSha.Any(static character => !Uri.IsHexDigit(character)))
                throw new InvalidOperationException("Git returned an invalid commit identifier.");

            var repositoryMetrics = EnsureRepositoryLimits(workspacePath, deleteOnViolation: true);
            return new WorkspaceSyncResult(
                workspacePath,
                commitSha.ToLowerInvariant(),
                branch,
                repositoryMetrics.SizeBytes);
        }
        catch
        {
            if (isNewWorkspace)
                DeleteWorkspace(workspacePath);
            throw;
        }
    }

    private async Task EnsureRemoteTreeFileCountAsync(
        string workspacePath,
        string treeish,
        CancellationToken cancellationToken)
    {
        // Tree objects contain paths but not reliable blob sizes. Asking Git for
        // `ls-tree -l` can lazily download omitted blobs in a partial clone, which
        // would defeat the pre-check. Stream NUL-delimited paths and terminate as
        // soon as the configured limit is exceeded instead of buffering the tree.
        var arguments = new[] { "-C", workspacePath, "ls-tree", "-r", "-z", "--name-only", treeish };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_settings.GitCommandTimeoutSeconds));
        using var process = new Process { StartInfo = CreateGitStartInfo(arguments) };

        _logger.LogInformation("Executing managed Git operation: {Operation}", GetOperationName(arguments));
        if (!process.Start())
            throw new InvalidOperationException("Unable to start the Git process.");

        var stderrTask = ReadBoundedAsync(
            process.StandardError,
            MaximumCapturedGitOutputCharacters,
            CancellationToken.None);

        try
        {
            var exceedsLimit = await ExceedsNullDelimitedEntryLimitAsync(
                process.StandardOutput.BaseStream,
                _settings.MaxFiles,
                timeout.Token);
            if (exceedsLimit)
            {
                TryKill(process);
                await WaitForExitAfterKillAsync(process);
                _ = await stderrTask;
                DeleteWorkspace(workspacePath);
                throw new RepositoryLimitExceededException("Repository exceeds the configured file-count limit.");
            }

            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            await WaitForExitAfterKillAsync(process);
            _ = await stderrTask;
            throw new TimeoutException("Git operation exceeded the configured timeout.");
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            await WaitForExitAfterKillAsync(process);
            _ = await stderrTask;
            throw;
        }

        var stderr = await stderrTask;
        if (process.ExitCode != 0)
        {
            _logger.LogWarning(
                "Managed Git operation {Operation} failed with exit code {ExitCode}. Error category: {ErrorCategory}",
                GetOperationName(arguments),
                process.ExitCode,
                ClassifyGitError(stderr));
            throw new InvalidOperationException($"Git operation failed with exit code {process.ExitCode}.");
        }
    }

    private async Task<string> RunGitAsync(
        IReadOnlyCollection<string> arguments,
        CancellationToken cancellationToken,
        string? monitoredWorkspacePath = null)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_settings.GitCommandTimeoutSeconds));
        using var monitorStop = new CancellationTokenSource();
        using var process = new Process { StartInfo = CreateGitStartInfo(arguments) };

        _logger.LogInformation("Executing managed Git operation: {Operation}", GetOperationName(arguments));
        if (!process.Start())
            throw new InvalidOperationException("Unable to start the Git process.");

        var stdoutTask = ReadBoundedAsync(
            process.StandardOutput,
            MaximumCapturedGitOutputCharacters,
            CancellationToken.None);
        var stderrTask = ReadBoundedAsync(
            process.StandardError,
            MaximumCapturedGitOutputCharacters,
            CancellationToken.None);
        var monitorTask = monitoredWorkspacePath is null
            ? Task.CompletedTask
            : MonitorRepositoryLimitsAsync(monitoredWorkspacePath, process, monitorStop.Token);

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            await WaitForExitAfterKillAsync(process);
            monitorStop.Cancel();
            await ObserveMonitorShutdownAsync(monitorTask, monitorStop.Token);
            _ = await stdoutTask;
            _ = await stderrTask;
            throw new TimeoutException("Git operation exceeded the configured timeout.");
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            await WaitForExitAfterKillAsync(process);
            monitorStop.Cancel();
            await ObserveMonitorShutdownAsync(monitorTask, monitorStop.Token);
            _ = await stdoutTask;
            _ = await stderrTask;
            throw;
        }
        finally
        {
            monitorStop.Cancel();
        }

        await ObserveMonitorShutdownAsync(monitorTask, monitorStop.Token);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (process.ExitCode != 0)
        {
            _logger.LogWarning(
                "Managed Git operation {Operation} failed with exit code {ExitCode}. Error category: {ErrorCategory}",
                GetOperationName(arguments),
                process.ExitCode,
                ClassifyGitError(stderr));
            throw new InvalidOperationException($"Git operation failed with exit code {process.ExitCode}.");
        }

        return stdout;
    }

    private async Task MonitorRepositoryLimitsAsync(
        string workspacePath,
        Process process,
        CancellationToken cancellationToken)
    {
        var interval = InitialLimitMonitorInterval;
        while (!process.HasExited)
        {
            await Task.Delay(interval, cancellationToken);
            if (process.HasExited)
                return;

            if (Directory.Exists(workspacePath))
            {
                try
                {
                    _ = CalculateRepositoryMetrics(workspacePath);
                }
                catch (RepositoryLimitExceededException)
                {
                    TryKill(process);
                    await WaitForExitAfterKillAsync(process);
                    DeleteWorkspace(workspacePath);
                    throw;
                }
            }

            interval = GetNextLimitMonitorInterval(interval);
        }
    }

    private static ProcessStartInfo CreateGitStartInfo(IReadOnlyCollection<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        ConfigureGitEnvironment(startInfo);
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);
        return startInfo;
    }

    internal static async Task<bool> ExceedsNullDelimitedEntryLimitAsync(
        Stream stream,
        int maximumEntries,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (maximumEntries < 0)
            throw new ArgumentOutOfRangeException(nameof(maximumEntries));

        var buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
        try
        {
            var entryCount = 0;
            while (true)
            {
                var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (bytesRead == 0)
                    return false;

                for (var index = 0; index < bytesRead; index++)
                {
                    if (buffer[index] != 0)
                        continue;

                    entryCount++;
                    if (entryCount > maximumEntries)
                        return true;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    internal static TimeSpan GetNextLimitMonitorInterval(TimeSpan current)
    {
        var nextSeconds = Math.Min(current.TotalSeconds * 2, MaximumLimitMonitorInterval.TotalSeconds);
        return TimeSpan.FromSeconds(nextSeconds);
    }

    private static async Task<string> ReadBoundedAsync(
        StreamReader reader,
        int maximumCharacters,
        CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<char>.Shared.Rent(4096);
        try
        {
            var builder = new StringBuilder(Math.Min(maximumCharacters, buffer.Length));
            while (true)
            {
                var read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read == 0)
                    return builder.ToString();

                var remaining = maximumCharacters - builder.Length;
                if (remaining > 0)
                    builder.Append(buffer, 0, Math.Min(read, remaining));
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    private static async Task ObserveMonitorShutdownAsync(Task monitorTask, CancellationToken monitorStopToken)
    {
        try
        {
            await monitorTask;
        }
        catch (OperationCanceledException) when (monitorStopToken.IsCancellationRequested)
        {
            // Normal monitor shutdown after the Git process exits or is terminated.
        }
    }

    private static async Task WaitForExitAfterKillAsync(Process process)
    {
        try
        {
            await process.WaitForExitAsync(CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // Process already exited or was never associated with a process handle.
        }
    }

    private async Task<ValidatedRemote> ValidateRemoteUrlAsync(
        string remoteUrl,
        CancellationToken cancellationToken)
    {
        var uri = ParseRemoteUri(remoteUrl);
        var trustedHost = _settings.AllowedRemoteHosts.Contains(uri.IdnHost, StringComparer.OrdinalIgnoreCase);

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
        }
        catch (SocketException exception)
        {
            throw new InvalidOperationException("The Git remote host could not be resolved.", exception);
        }

        var normalizedAddresses = addresses
            .Select(NormalizeAddress)
            .Distinct()
            .ToArray();
        if (normalizedAddresses.Length == 0)
            throw new InvalidOperationException("The Git remote host could not be resolved.");

        if (!trustedHost && !_settings.AllowPrivateNetworkRemotes && normalizedAddresses.Any(IsRestrictedAddress))
            throw new InvalidOperationException("Private-network Git remotes are disabled.");

        var selectedAddress = normalizedAddresses
            .OrderBy(static address => address.AddressFamily == AddressFamily.InterNetwork ? 0 : 1)
            .ThenBy(static address => address.ToString(), StringComparer.Ordinal)
            .First();

        return new ValidatedRemote(uri, selectedAddress);
    }

    private static Uri ParseRemoteUri(string remoteUrl)
    {
        if (!Uri.TryCreate(remoteUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("Only absolute HTTPS Git URLs are supported for linked repositories.", nameof(remoteUrl));
        if (!string.IsNullOrEmpty(uri.UserInfo) || string.IsNullOrWhiteSpace(uri.Host))
            throw new ArgumentException("Git URLs must not contain embedded credentials.", nameof(remoteUrl));
        if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new ArgumentException("Git URLs must not contain query strings or fragments.", nameof(remoteUrl));

        return uri;
    }

    private static IReadOnlyCollection<string> BuildNetworkArguments(
        ValidatedRemote remote,
        params string[] operationArguments)
    {
        var arguments = new List<string>
        {
            "-c", "credential.helper=",
            "-c", "http.followRedirects=false",
            "-c", "http.proxy=",
            "-c", $"http.curloptResolve={remote.CurlResolveEntry}"
        };
        arguments.AddRange(operationArguments);
        return arguments;
    }

    private static void ConfigureGitEnvironment(ProcessStartInfo startInfo)
    {
        startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
        startInfo.Environment["GIT_OPTIONAL_LOCKS"] = "0";
        startInfo.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        startInfo.Environment["GIT_CONFIG_GLOBAL"] = OperatingSystem.IsWindows() ? "NUL" : "/dev/null";
        startInfo.Environment["GIT_PROTOCOL_FROM_USER"] = "0";
        startInfo.Environment["GIT_ALLOW_PROTOCOL"] = "https";

        foreach (var variable in new[] { "HTTP_PROXY", "HTTPS_PROXY", "ALL_PROXY", "http_proxy", "https_proxy", "all_proxy" })
            startInfo.Environment.Remove(variable);
    }

    internal static bool IsRestrictedAddress(IPAddress address)
    {
        address = NormalizeAddress(address);
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            return true;

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 0 ||
                   bytes[0] == 10 ||
                   bytes[0] == 127 ||
                   (bytes[0] == 100 && bytes[1] is >= 64 and <= 127) ||
                   (bytes[0] == 169 && bytes[1] == 254) ||
                   (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 0) ||
                   (bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 2) ||
                   (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 198 && bytes[1] is 18 or 19) ||
                   (bytes[0] == 198 && bytes[1] == 51 && bytes[2] == 100) ||
                   (bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113) ||
                   bytes[0] >= 224;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();
            return address.IsIPv6LinkLocal ||
                   address.IsIPv6Multicast ||
                   (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0xc0) ||
                   address.Equals(IPAddress.IPv6Loopback) ||
                   (bytes[0] & 0xfe) == 0xfc ||
                   (bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8);
        }

        return true;
    }

    private static IPAddress NormalizeAddress(IPAddress address)
        => address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;

    private static void ValidateBranch(string branch)
    {
        if (string.IsNullOrWhiteSpace(branch) ||
            branch.Length > 150 ||
            branch.StartsWith("-", StringComparison.Ordinal) ||
            branch.StartsWith("/", StringComparison.Ordinal) ||
            branch.EndsWith("/", StringComparison.Ordinal) ||
            branch.EndsWith(".", StringComparison.Ordinal) ||
            branch.Contains("..", StringComparison.Ordinal) ||
            branch.Contains("@{", StringComparison.Ordinal) ||
            branch.Contains("//", StringComparison.Ordinal) ||
            branch.Any(static character => char.IsControl(character) || character is ' ' or '~' or '^' or ':' or '?' or '*' or '[' or '\\'))
        {
            throw new ArgumentException("Branch name is not a safe Git branch reference.", nameof(branch));
        }
    }

    private static string ClassifyGitError(string stderr)
    {
        if (stderr.Contains("Authentication failed", StringComparison.OrdinalIgnoreCase) ||
            stderr.Contains("could not read Username", StringComparison.OrdinalIgnoreCase))
            return "authentication";
        if (stderr.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
            stderr.Contains("Remote branch", StringComparison.OrdinalIgnoreCase))
            return "not_found";
        return "git_failure";
    }

    private static string GetOperationName(IReadOnlyCollection<string> arguments)
        => arguments.FirstOrDefault(argument => argument is "clone" or "fetch" or "checkout" or "reset" or "clean" or "rev-parse" or "remote")
           ?? "git";

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Process already exited.
        }
    }

    private static void EnsureChildPath(string root, string candidate)
    {
        var normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!candidate.StartsWith(normalizedRoot, comparison))
            throw new InvalidOperationException("Workspace path escaped the configured root.");
    }

    private RepositoryMetrics EnsureRepositoryLimits(string workspacePath, bool deleteOnViolation)
    {
        try
        {
            return CalculateRepositoryMetrics(workspacePath);
        }
        catch (RepositoryLimitExceededException)
        {
            if (deleteOnViolation)
                DeleteWorkspace(workspacePath);
            throw;
        }
    }

    private RepositoryMetrics CalculateRepositoryMetrics(string path)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        long sizeBytes = 0;
        var fileCount = 0;
        foreach (var file in Directory.EnumerateFiles(path, "*", options))
        {
            fileCount++;
            if (fileCount > _settings.MaxFiles)
                throw new RepositoryLimitExceededException("Repository exceeds the configured file-count limit.");

            try
            {
                sizeBytes = checked(sizeBytes + new FileInfo(file).Length);
            }
            catch (FileNotFoundException)
            {
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                continue;
            }
            catch (OverflowException)
            {
                throw new RepositoryLimitExceededException("Repository exceeds the configured processing size limit.");
            }

            if (sizeBytes > _settings.MaxRepositoryBytes)
                throw new RepositoryLimitExceededException("Repository exceeds the configured processing size limit.");
        }

        return new RepositoryMetrics(sizeBytes, fileCount);
    }

    private static void DeleteWorkspace(string workspacePath)
    {
        if (!Directory.Exists(workspacePath))
            return;

        try
        {
            Directory.Delete(workspacePath, recursive: true);
        }
        catch (IOException)
        {
            // Cleanup is best effort; the processing error is preserved.
        }
        catch (UnauthorizedAccessException)
        {
            // Cleanup is best effort; the processing error is preserved.
        }
    }

    private sealed record ValidatedRemote(Uri Uri, IPAddress Address)
    {
        private int Port => Uri.IsDefaultPort ? 443 : Uri.Port;
        private string AddressText => Address.AddressFamily == AddressFamily.InterNetworkV6
            ? $"[{Address}]"
            : Address.ToString();

        public string CurlResolveEntry => $"{Uri.IdnHost}:{Port}:{AddressText}";
    }

    private sealed record RepositoryMetrics(long SizeBytes, int FileCount);

    private sealed class RepositoryLimitExceededException : InvalidOperationException
    {
        public RepositoryLimitExceededException(string message) : base(message) { }
    }

    private sealed class RepositoryLockEntry
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public int ReferenceCount { get; set; }
    }
}
