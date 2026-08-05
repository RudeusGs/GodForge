using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Core;

public sealed class ProjectSetting : BaseAuditableEntity
{
    public Guid ProjectId { get; private set; }
    public string AnalysisProfileKey { get; private set; } = default!;
    public bool AiAdvisoryEnabled { get; private set; }
    public string DefaultAssetVisibility { get; private set; } = default!;
    public int NotificationPolicyVersion { get; private set; }
    public long Version { get; private set; }

    private ProjectSetting() { }

    public static ProjectSetting Create(Guid projectId, DateTimeOffset now)
        => new()
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            AnalysisProfileKey = "current-default-v1",
            AiAdvisoryEnabled = false,
            DefaultAssetVisibility = "private",
            NotificationPolicyVersion = 1,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void Update(string analysisProfileKey, bool aiAdvisoryEnabled, string defaultAssetVisibility, int notificationPolicyVersion, long expectedVersion, DateTimeOffset now)
    {
        if (Version != expectedVersion)
            throw new InvalidOperationException("Concurrency version mismatch.");
        ArgumentException.ThrowIfNullOrWhiteSpace(analysisProfileKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultAssetVisibility);
        AnalysisProfileKey = analysisProfileKey.Trim();
        AiAdvisoryEnabled = aiAdvisoryEnabled;
        DefaultAssetVisibility = defaultAssetVisibility.Trim().ToLowerInvariant();
        NotificationPolicyVersion = notificationPolicyVersion;
        Version++;
        UpdatedAt = now;
    }
}