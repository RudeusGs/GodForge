using System;
using System.Collections.Generic;
using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Repo;

public sealed class GitCommitFile : BaseEntity
{
    public Guid CommitId { get; private set; }
    public string FilePath { get; private set; }
    public string? OldFilePath { get; private set; }
    public string ChangeType { get; private set; }
    public int? Additions { get; private set; }
    public int? Deletions { get; private set; }

    private GitCommitFile()
    {
        FilePath = null!;
        ChangeType = null!;
    }
}
