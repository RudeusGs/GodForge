using System.Diagnostics;
using System.Globalization;
using System.Text;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Models.Analysis;
using GodForge.Application.Features.Repositories;

namespace GodForge.Infrastructure.Git;

public sealed partial class GitWorkspaceService
{
    public async Task<WorkspaceSyncResult> SynchronizeAsync(Guid repositoryId, string remoteUrl, string defaultBranch, CancellationToken token)
    {
        if (!RepositoryInput.ValidBranch(defaultBranch)) throw new HostedGitException("REPOSITORY_REF_INVALID");
        var remote = await ValidateRemoteUrlAsync(remoteUrl, token);
        var directory = BrowseDirectory(repositoryId, false);
        await using var lease = await _repositoryLockProvider.AcquireAsync(repositoryId, TimeSpan.FromSeconds(15), token);
        Directory.CreateDirectory(directory);
        if (!File.Exists(Path.Combine(directory, "HEAD")))
            await RunGitAsync(new[] { "init", "--bare", "--", directory }, token);
        // Anonymous HTTPS only; existing DNS pinning, redirect rejection, process
        // limits and repository byte monitoring apply to every network fetch.
        await RunGitAsync(BuildNetworkArguments(remote, "-C", directory, "fetch", "--prune", "--no-tags", "--depth=200",
            "--no-write-fetch-head", "--", remoteUrl, "+refs/heads/*:refs/heads/*"), token, directory);
        await EnsureRemoteTreeFileCountAsync(directory, $"refs/heads/{defaultBranch}", token);
        var sha = (await ReadLocalAsync(directory, new[] { "rev-parse", $"refs/heads/{defaultBranch}" }, token)).Trim();
        if (!RepositoryInput.ValidSha(sha)) throw new HostedGitException("REPOSITORY_REF_INVALID");
        return new(directory, sha, defaultBranch, EnsureRepositoryLimits(directory, false).SizeBytes);
    }

    public async Task<IReadOnlyList<RepositoryBranchDto>> BranchesAsync(Guid repositoryId, string defaultBranch, int page, int pageSize, CancellationToken token)
    {
        await using var lease = await _repositoryLockProvider.AcquireAsync(repositoryId, TimeSpan.FromSeconds(10), token);
        var value = await ReadLocalAsync(BrowseDirectory(repositoryId), new[] { "for-each-ref", "--count=1001", "--format=%(refname:strip=2)%00%(objectname)", "refs/heads/" }, token);
        var entries = value.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (entries.Length > 1000) throw new HostedGitException("REPOSITORY_TREE_TOO_LARGE");
        return entries.Skip((page - 1) * pageSize).Take(pageSize).Select(line =>
        {
            var pair = line.TrimEnd('\r').Split('\0');
            if (pair.Length != 2 || !RepositoryInput.ValidBranch(pair[0]) || !RepositoryInput.ValidSha(pair[1])) throw new HostedGitException("REPOSITORY_REF_INVALID");
            return new RepositoryBranchDto(pair[0], pair[1], pair[0] == defaultBranch, false);
        }).ToArray();
    }

    public async Task<IReadOnlyList<RepositoryCommitDto>> CommitsAsync(Guid repositoryId, string branch, int page, int pageSize, CancellationToken token)
    {
        if (!RepositoryInput.ValidBranch(branch)) throw new HostedGitException("REPOSITORY_REF_INVALID");
        await using var lease = await _repositoryLockProvider.AcquireAsync(repositoryId, TimeSpan.FromSeconds(10), token);
        var output = await ReadLocalAsync(BrowseDirectory(repositoryId), new[] { "log", $"--max-count={pageSize}", $"--skip={(page - 1) * pageSize}",
            "--format=%H%x00%T%x00%an%x00%ae%x00%aI%x00%B%x00%P%x00", $"refs/heads/{branch}", "--" }, token);
        var parts = output.Split('\0');
        var result = new List<RepositoryCommitDto>();
        for (var i = 0; i + 7 < parts.Length; i += 7)
        {
            var sha = parts[i].Trim();
            if (!RepositoryInput.ValidSha(sha) || !RepositoryInput.ValidSha(parts[i + 1])) throw new HostedGitException("REPOSITORY_REF_INVALID");
            result.Add(new(sha, sha[..8], parts[i + 5].TrimEnd(), parts[i + 2], DateTimeOffset.Parse(parts[i + 4], CultureInfo.InvariantCulture),
                parts[i + 6].Split(' ', StringSplitOptions.RemoveEmptyEntries), parts[i + 1], parts[i + 3]));
        }
        return result;
    }

    public async Task<RepositoryTreeDto> TreeAsync(Guid repositoryId, string sha, string path, CancellationToken token)
    {
        ValidateBrowseInput(sha, path, true);
        await using var lease = await _repositoryLockProvider.AcquireAsync(repositoryId, TimeSpan.FromSeconds(10), token);
        var directory = BrowseDirectory(repositoryId);
        await RequireCommitAsync(directory, sha, token);
        var tree = path.Length == 0 ? sha : sha + ":" + path;
        var output = await ReadLocalAsync(directory, new[] { "ls-tree", "-z", "-l", tree }, token);
        return new(sha, ParseTree(output, path));
    }

    public async Task<RepositoryBlobDto> BlobAsync(Guid repositoryId, string sha, string path, CancellationToken token)
    {
        ValidateBrowseInput(sha, path, false);
        await using var lease = await _repositoryLockProvider.AcquireAsync(repositoryId, TimeSpan.FromSeconds(10), token);
        var directory = BrowseDirectory(repositoryId);
        await RequireCommitAsync(directory, sha, token);
        var entry = ParseTree(await ReadLocalAsync(directory, new[] { "ls-tree", "-z", "-l", sha, "--", path }, token), "").SingleOrDefault(e => e.Path == path);
        if (entry is null || entry.Type != "blob") throw new HostedGitException("REPOSITORY_PATH_INVALID");
        var maximum = Math.Min(_settings.MaxTextFileBytes, 512 * 1024);
        if (entry.Size > maximum) throw new HostedGitException("REPOSITORY_BLOB_TOO_LARGE");
        var bytes = await ReadLocalBytesAsync(directory, new[] { "cat-file", "blob", entry.Sha }, maximum + 1, token);
        if (bytes.Length > maximum) throw new HostedGitException("REPOSITORY_BLOB_TOO_LARGE");
        if (bytes.Any(b => b == 0 || b < 9 || b is > 13 and < 32)) throw new HostedGitException("REPOSITORY_BINARY_FILE");
        try { return new(sha, path, new UTF8Encoding(false, true).GetString(bytes), bytes.Length); }
        catch (DecoderFallbackException) { throw new HostedGitException("REPOSITORY_BINARY_FILE"); }
    }

    private string BrowseDirectory(Guid id, bool require = true)
    {
        if (id == Guid.Empty) throw new HostedGitException("REPOSITORY_NOT_CONNECTED");
        var root = Path.GetFullPath(_settings.WorkspaceRoot);
        var directory = Path.GetFullPath(Path.Combine(root, id.ToString("N") + "-browse"));
        EnsureChildPath(root, directory);
        if (Directory.Exists(directory) && (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
            throw new HostedGitException("REPOSITORY_PATH_INVALID");
        if (require && !File.Exists(Path.Combine(directory, "HEAD"))) throw new HostedGitException("REPOSITORY_SYNC_REQUIRED");
        return directory;
    }
    private static void ValidateBrowseInput(string sha, string path, bool empty)
    {
        if (!RepositoryInput.ValidSha(sha)) throw new HostedGitException("REPOSITORY_REF_INVALID");
        if (!RepositoryInput.ValidPath(path, empty)) throw new HostedGitException("REPOSITORY_PATH_INVALID");
    }
    private async Task RequireCommitAsync(string directory, string sha, CancellationToken token)
    {
        if ((await ReadLocalAsync(directory, new[] { "cat-file", "-t", sha }, token)).Trim() != "commit")
            throw new HostedGitException("REPOSITORY_REF_INVALID");
    }
    private static RepositoryTreeEntryDto[] ParseTree(string output, string parent)
    {
        var records = output.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        if (records.Length > 1000) throw new HostedGitException("REPOSITORY_TREE_TOO_LARGE");
        return records.Select(record =>
        {
            var tab = record.IndexOf('\t');
            if (tab < 0) throw new HostedGitException("REPOSITORY_PATH_INVALID");
            var fields = record[..tab].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var name = record[(tab + 1)..];
            var type = fields[0] is "120000" or "160000" ? "unsupported" : fields[1];
            return new RepositoryTreeEntryDto(name.Split('/').Last(), parent.Length == 0 ? name : parent + "/" + name, type,
                long.TryParse(fields[3], out var size) ? size : 0, fields[2]);
        }).ToArray();
    }
    private async Task<string> ReadLocalAsync(string directory, string[] args, CancellationToken token)
    {
        try { return new UTF8Encoding(false, true).GetString(await ReadLocalBytesAsync(directory, args, 4 * 1024 * 1024, token)); }
        catch (DecoderFallbackException) { throw new HostedGitException("REPOSITORY_PATH_INVALID"); }
    }
    private async Task<byte[]> ReadLocalBytesAsync(string directory, string[] args, int maximum, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Min(30, _settings.GitCommandTimeoutSeconds)));
        var arguments = new[] { "--literal-pathspecs", "-C", directory }.Concat(args).ToArray();
        using var process = new Process { StartInfo = CreateGitStartInfo(arguments) };
        process.StartInfo.Environment["GIT_NO_LAZY_FETCH"] = "1";
        process.Start();
        var error = ReadBoundedAsync(process.StandardError, 4096, CancellationToken.None);
        using var memory = new MemoryStream();
        try
        {
            var chunk = new byte[8192]; int read;
            while ((read = await process.StandardOutput.BaseStream.ReadAsync(chunk, timeout.Token)) != 0)
            {
                if (memory.Length + read > maximum) throw new HostedGitException("REPOSITORY_BLOB_TOO_LARGE");
                memory.Write(chunk, 0, read);
            }
            await process.WaitForExitAsync(timeout.Token); await error;
            if (process.ExitCode != 0) throw new HostedGitException("REPOSITORY_REF_INVALID");
            return memory.ToArray();
        }
        finally { if (!process.HasExited) { TryKill(process); await WaitForExitAfterKillAsync(process); } await error; }
    }
}
