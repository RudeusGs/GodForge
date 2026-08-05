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
                postgres.ConstraintName,
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
        if (_transaction is null)
            throw new InvalidOperationException("No unit-of-work transaction is active.");
        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            return;
        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    private static long CreateAdvisoryLockKey(string resourceType, Guid resourceId)
    {
        var input = Encoding.UTF8.GetBytes($"{resourceType.Trim().ToLowerInvariant()}:{resourceId:N}");
        var hash = SHA256.HashData(input);
        return BinaryPrimitives.ReadInt64LittleEndian(hash);
    }
}
