using System.Diagnostics;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models.Analysis;
using GodForge.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GodForge.Infrastructure.Git;

public sealed class GitWorkspaceService : IRepositoryWorkspaceService
{
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
        ValidateRemoteUrl(remoteUrl);
        if (string.IsNullOrWhiteSpace(branch))
        {
            throw new ArgumentException("Branch is required.", nameof(branch));
        }

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
                new[] { "clone", "--no-tags", "--single-branch", "--branch", branch, remoteUrl, workspacePath },
                cancellationToken);
        }
        else
        {
            await RunGitAsync(new[] { "-C", workspacePath, "remote", "set-url", "origin", remoteUrl }, cancellationToken);
            await RunGitAsync(new[] { "-C", workspacePath, "fetch", "--prune", "origin", branch }, cancellationToken);
            await RunGitAsync(new[] { "-C", workspacePath, "checkout", "-B", branch, $"origin/{branch}" }, cancellationToken);
            await RunGitAsync(new[] { "-C", workspacePath, "reset", "--hard", $"origin/{branch}" }, cancellationToken);
            await RunGitAsync(new[] { "-C", workspacePath, "clean", "-ffd" }, cancellationToken);
        }

        var commitSha = (await RunGitAsync(new[] { "-C", workspacePath, "rev-parse", "HEAD" }, cancellationToken)).Trim();
        var repositorySize = CalculateDirectorySize(workspacePath);
        if (repositorySize > _settings.MaxRepositoryBytes)
        {
            throw new InvalidOperationException("Repository exceeds the configured processing size limit.");
        }

        return new WorkspaceSyncResult(workspacePath, commitSha, branch, repositorySize);
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
        _ = await stderrTask;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Git operation failed with exit code {process.ExitCode}.");
        }

        return stdout;
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

    private static void ValidateRemoteUrl(string remoteUrl)
    {
        if (!Uri.TryCreate(remoteUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new ArgumentException("Only absolute HTTP(S) Git URLs are supported by the MVP workspace adapter.", nameof(remoteUrl));
        }
    }

    private static void EnsureChildPath(string root, string candidate)
    {
        var normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(normalizedRoot, StringComparison.Ordinal))
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
