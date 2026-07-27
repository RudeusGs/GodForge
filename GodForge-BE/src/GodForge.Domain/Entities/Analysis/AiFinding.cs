using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Analysis;

public sealed class AiFinding : BaseEntity
{
    public Guid RunId { get; private set; }
    public string Category { get; private set; } = default!;
    public string Severity { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string? Recommendation { get; private set; }
    public decimal? Confidence { get; private set; }
    public string EvidenceRefsJson { get; private set; } = "[]";
    public string Fingerprint { get; private set; } = default!;
    public string Status { get; private set; } = "open";
    public DateTimeOffset CreatedAt { get; private set; }

    private AiFinding() { }

    public static AiFinding Create(
        Guid runId,
        string category,
        string severity,
        string title,
        string description,
        string? recommendation,
        decimal? confidence,
        string evidenceRefsJson,
        string fingerprint,
        DateTimeOffset now)
    {
        return new AiFinding
        {
            Id = Guid.NewGuid(),
            RunId = runId,
            Category = category,
            Severity = severity,
            Title = title,
            Description = description,
            Recommendation = recommendation,
            Confidence = confidence,
            EvidenceRefsJson = evidenceRefsJson,
            Fingerprint = fingerprint,
            CreatedAt = now
        };
    }
}
