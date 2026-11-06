namespace GodForge.Application.Common.Models;

public sealed class HostedGitException(string code, bool retryable = false) : Exception(code)
{
    public string Code { get; } = code;
    public bool Retryable { get; } = retryable;
}

public sealed record RepositoryBranchDto(string Name, string CommitSha, bool IsDefault, bool IsProtected);
public sealed record RepositoryCommitDto(string Sha, string ShortSha, string Message, string Author, DateTimeOffset Timestamp, IReadOnlyList<string> Parents, string TreeSha = "", string AuthorEmail = "");
public sealed record RepositoryTreeEntryDto(string Name, string Path, string Type, long Size, string Sha);
public sealed record RepositoryTreeDto(string CommitSha, IReadOnlyList<RepositoryTreeEntryDto> Entries);
public sealed record RepositoryBlobDto(string CommitSha, string Path, string Content, long Size);
public sealed record ForgejoUserDto(long Id, string Login);
public sealed record ForgejoCollaboratorDto(long Id, string Login, string Permission);
