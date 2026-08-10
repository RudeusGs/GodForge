namespace GodForge.Domain.Common;

public interface ISoftDeletable
{
    public DateTimeOffset? DeletedAt { get; }
    public void SoftDelete(DateTimeOffset now);
}
