using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models.Analysis;
using GodForge.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GodForge.Infrastructure.Git;

public sealed class GitWorkspaceService : IRepositoryWorkspaceService
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> RepositoryLocks = new();
    private readonly RepositoryProcessingSettings _settings;
    private readonly ILogger<GitWorkspaceService> _logger;

    public GitWorkspaceService(
        IOptions<RepositoryProcessingSettings> settings,
        ILogger<GitWorkspaceService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<WorkspaceSyncResult> SyncAsync(
        Guid repositoryId,
        string remoteUrl,
        string branch,
        CancellationToken cancellationToken = default)
    {
        await ValidateRemoteUrlAsync(remoteUrl, cancellationToken);
        ValidateBranch(branch);

        var gate = RepositoryLocks.GetOrAdd(repositoryId, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await SyncCoreAsync(repositoryId, remoteUrl, branch, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
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
        if (!Directory.Exists(gitDirectory))
        {
            if (Directory.Exists(workspacePath))
            {
                Directory.Delete(workspacePath, recursive: true);
            }

            await RunGitAsync(
                new[]
                {
                    "-c", "credential.helper=", "clone", "--no-tags", "--single-branch",
                    "--branch", branch, "--", remoteUrl, workspacePath
                },
                cancellationToken);
        }
        else
        {
            await RunGitAsync(new[] { "-C", workspacePath, "remote", "set-url", "origin", remoteUrl }, cancellationToken);
            await RunGitAsync(new[] { "-c", "credential.helper=", "-C", workspacePath, "fetch", "--prune", "origin", branch }, cancellationToken);
            await RunGitAsync(new[] { "-C", workspacePath, "checkout", "-B", branch, $"origin/{branch}" }, cancellationToken);
            await RunGitAsync(new[] { "-C", workspacePath, "reset", "--hard", $"origin/{branch}" }, cancellationToken);
            await RunGitAsync(new[] { "-C", workspacePath, "clean", "-ffd" }, cancellationToken);
        }

        var commitSha = (await RunGitAsync(new[] { "-C", workspacePath, "rev-parse", "HEAD" }, cancellationToken)).Trim();
        if (commitSha.Length != 40 || commitSha.Any(static character => !Uri.IsHexDigit(character)))
        {
            throw new InvalidOperationException("Git returned an invalid commit identifier.");
        }

        var repositorySize = CalculateDirectorySize(workspacePath);
        if (repositorySize > _settings.MaxRepositoryBytes)
        {
            throw new InvalidOperationException("Repository exceeds the configured processing size limit.");
        }

        return new WorkspaceSyncResult(workspacePath, commitSha.ToLowerInvariant(), branch, repositorySize);
    }

    private async Task<string> RunGitAsync(IReadOnlyCollection<string> arguments, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_settings.GitCommandTimeoutSeconds));

        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
        startInfo.Environment["GIT_OPTIONAL_LOCKS"] = "0";

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        _logger.LogInformation("Executing managed Git operation: {Operation}", arguments.FirstOrDefault() ?? "unknown");

        if (!process.Start())
        {
            throw new InvalidOperationException("Unable to start the Git process.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(timeout.Token);

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (process.ExitCode != 0)
        {
            _logger.LogWarning(
                "Managed Git operation {Operation} failed with exit code {ExitCode}. Error category: {ErrorCategory}",
                arguments.FirstOrDefault() ?? "unknown",
                process.ExitCode,
                ClassifyGitError(stderr));
            throw new InvalidOperationException($"Git operation failed with exit code {process.ExitCode}.");
        }

        return stdout;
    }

    private async Task ValidateRemoteUrlAsync(string remoteUrl, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(remoteUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Only absolute HTTPS Git URLs are supported for linked repositories.", nameof(remoteUrl));
        }

        if (!string.IsNullOrEmpty(uri.UserInfo) || string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new ArgumentException("Git URLs must not contain embedded credentials.", nameof(remoteUrl));
        }

        if (_settings.AllowedRemoteHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        if (_settings.AllowPrivateNetworkRemotes)
        {
            return;
        }

        if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Private-network Git remotes are disabled.");
        }

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
        }
        catch (SocketException exception)
        {
            throw new InvalidOperationException("The Git remote host could not be resolved.", exception);
        }

        if (addresses.Length == 0 || addresses.Any(IsPrivateAddress))
        {
            throw new InvalidOperationException("Private-network Git remotes are disabled.");
        }
    }

    private static bool IsPrivateAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 10 ||
                   bytes[0] == 127 ||
                   (bytes[0] == 169 && bytes[1] == 254) ||
                   (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 100 && bytes[1] is >= 64 and <= 127);
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return address.IsIPv6LinkLocal ||
                   address.Equals(IPAddress.IPv6Loopback) ||
                   (address.GetAddressBytes()[0] & 0xfe) == 0xfc;
        }

        return true;
    }

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
        {
            return "authentication";
        }

        if (stderr.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
            stderr.Contains("Remote branch", StringComparison.OrdinalIgnoreCase))
        {
            return "not_found";
        }

        return "git_failure";
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
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
        {
            throw new InvalidOperationException("Workspace path escaped the configured root.");
        }
    }

    private static long CalculateDirectorySize(string path)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        return Directory.EnumerateFiles(path, "*", options)
            .Sum(file => new FileInfo(file).Length);
    }
}
