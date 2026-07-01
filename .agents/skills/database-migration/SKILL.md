---
name: database-migration
description: Skill for creating and managing Entity Framework Core database migrations for the GodForge PostgreSQL database. Covers entity configuration, snake_case naming, index strategy, and migration best practices.
---

# Database Migration

Skill hướng dẫn tạo và quản lý EF Core database migrations.

## Quy trình

### 1. Đọc schema trước
- Mở `docs/SRS/04-database.md` và tìm bảng cần tạo/sửa.
- Xác nhận: column names, types, nullability, constraints, indexes.
- Table và column name trong PostgreSQL phải là **snake_case**.

### 2. Tạo Entity Configuration

Đặt trong `GodForge.Infrastructure/Persistence/Configurations/`.

```csharp
namespace GodForge.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.Property(p => p.DeletedAt)
            .HasColumnName("deleted_at");

        // Indexes
        builder.HasIndex(p => p.Name)
            .IsUnique()
            .HasDatabaseName("idx_projects_name");

        builder.HasIndex(p => p.DeletedAt)
            .HasDatabaseName("idx_projects_deleted_at");

        // Query filter for soft-delete
        builder.HasQueryFilter(p => p.DeletedAt == null);
    }
}
```

### 3. Tạo Migration

```bash
cd GodForge-BE
dotnet ef migrations add <MigrationName> --project src/GodForge.Infrastructure --startup-project src/GodForge.Api
```

**Naming convention cho migration:** `PascalCase` mô tả thay đổi.
- `AddUsersTable`
- `AddProjectMembersTable`
- `AddHealthScoreToProjects`

### 4. Review migration file
- Kiểm tra SQL được generate có đúng với schema trong `04-database.md`.
- Đảm bảo có cả `Up` và `Down` method.
- Kiểm tra index names khớp với tài liệu.

### 5. Apply migration

```bash
dotnet ef database update --project src/GodForge.Infrastructure --startup-project src/GodForge.Api
```

## Lưu ý quan trọng
- **KHÔNG** sửa migration file đã được apply vào DB. Tạo migration mới để sửa.
- **KHÔNG** dùng `EnsureCreated()` — luôn dùng migrations.
- Column type mapping: `Guid` → `uuid`, `string` → `varchar(n)`, `DateTimeOffset` → `timestamptz`, `int` → `integer`, `long` → `bigint`, `bool` → `boolean`, `Dictionary/Object` → `jsonb`.
- Soft-delete: cấu hình global query filter `HasQueryFilter(e => e.DeletedAt == null)`.
- FK cascade rules: Core schema dùng `ON DELETE CASCADE` cho owned entities, `ON DELETE SET NULL` cho references.
