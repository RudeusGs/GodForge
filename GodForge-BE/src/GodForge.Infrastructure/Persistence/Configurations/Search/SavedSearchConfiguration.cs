using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Entities.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GodForge.Infrastructure.Persistence.Configurations.Search;

public sealed class SavedSearchConfiguration : IEntityTypeConfiguration<SavedSearch>
{
    public void Configure(EntityTypeBuilder<SavedSearch> builder)
    {
        builder.ToTable("saved_searches", "search");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasColumnType("uuid");

        builder.Property(s => s.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.Property(s => s.Query).HasColumnName("query").HasColumnType("text").IsRequired();
        builder.Property(s => s.FiltersJson).HasColumnName("filters").HasColumnType("jsonb");

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
