using GodForge.Domain.Entities.Governance;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Governance;

public sealed class LegalHoldConfiguration : IEntityTypeConfiguration<LegalHold>
{
    public void Configure(EntityTypeBuilder<LegalHold> builder)
    {
        builder.ToTable("legal_holds", "governance");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(h => h.TargetType).HasColumnName("target_type").HasMaxLength(80).IsRequired();
        builder.Property(h => h.TargetId).HasColumnName("target_id").HasColumnType("uuid").IsRequired();
        builder.Property(h => h.Reason).HasColumnName("reason").HasColumnType("text").IsRequired();
        builder.Property(h => h.CreatedBy).HasColumnName("created_by").HasColumnType("uuid").IsRequired();

        builder.Property(h => h.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(h => h.ReleasedAt).HasColumnName("released_at").HasColumnType("timestamptz");

        builder.HasOne<User>().WithMany().HasForeignKey(h => h.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
