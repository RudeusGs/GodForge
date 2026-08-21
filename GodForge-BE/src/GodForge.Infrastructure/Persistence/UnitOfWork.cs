using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace GodForge.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly GodForgeDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(GodForgeDbContext context) => _context = context;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException("The resource changed concurrently.", exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres)
        {
            throw new UniqueConstraintConflictException(
                "A unique database constraint was violated.",
                MapUniqueConstraint(postgres.ConstraintName),
                exception);
        }
    }

    public void ClearTrackedChanges() => _context.ChangeTracker.Clear();

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException("A unit-of-work transaction is already active.");
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task AcquireResourceLockAsync(
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("A transaction must be active before acquiring a resource lock.");
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);

        var lockKey = CreateAdvisoryLockKey(resourceType, resourceId);
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({lockKey})",
            cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = _transaction ??
            throw new InvalidOperationException("No unit-of-work transaction is active.");

        try
        {
            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await DisposeTransactionAsync(transaction);
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var transaction = _transaction;
        if (transaction is null)
            return;

        // Transaction cleanup must still complete after the request token is cancelled.
        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        finally
        {
            await DisposeTransactionAsync(transaction);
        }
    }

    private async Task DisposeTransactionAsync(IDbContextTransaction transaction)
    {
        try
        {
            await transaction.DisposeAsync();
        }
        finally
        {
            _transaction = null;
        }
    }

    private static UniqueConstraintKind MapUniqueConstraint(string? constraintName)
        => constraintName switch
        {
            "ux_auth_challenges_active_scope" => UniqueConstraintKind.AuthChallengeActiveScope,
            "ux_users_normalized_email" => UniqueConstraintKind.UserNormalizedEmail,
            "ux_organizations_slug" => UniqueConstraintKind.OrganizationSlug,
            "ux_user_invites_active_org_email" => UniqueConstraintKind.UserInviteActiveOrganizationEmail,
            "ux_idempotency_records_scope" => UniqueConstraintKind.IdempotencyScope,
            "ux_projects_org_slug_active" => UniqueConstraintKind.ProjectOrganizationSlug,
            "ux_projects_org_upper_name_active" => UniqueConstraintKind.ProjectOrganizationName,
            "ux_project_members_project_user" => UniqueConstraintKind.ProjectMemberUser,
            "ux_repositories_project" => UniqueConstraintKind.RepositoryProject,
            _ => UniqueConstraintKind.Unknown
        };

    private static long CreateAdvisoryLockKey(string resourceType, Guid resourceId)
    {
        var input = Encoding.UTF8.GetBytes($"{resourceType.Trim().ToLowerInvariant()}:{resourceId:N}");
        var hash = SHA256.HashData(input);
        return BinaryPrimitives.ReadInt64LittleEndian(hash);
    }
}
