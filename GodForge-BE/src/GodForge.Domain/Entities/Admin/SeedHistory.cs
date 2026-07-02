using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Admin;

public sealed class SeedHistory : BaseEntity
{
    public string SeedName { get; private set; } = default!;
    public string Checksum { get; private set; } = default!;
    public DateTimeOffset AppliedAt { get; private set; }

    private SeedHistory() { } // EF Core

    public static SeedHistory Create(
        string seedName, string checksum, DateTimeOffset appliedAt)
    {
        return new SeedHistory
        {
            Id = Guid.NewGuid(),
            SeedName = seedName,
            Checksum = checksum,
            AppliedAt = appliedAt
        };
    }
}
