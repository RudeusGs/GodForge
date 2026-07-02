namespace GodForge.Domain.Common;

public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; }
    void SoftDelete(DateTimeOffset now);
}
