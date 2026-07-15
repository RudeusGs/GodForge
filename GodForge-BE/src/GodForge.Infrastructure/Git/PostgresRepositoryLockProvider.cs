using GodForge.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace GodForge.Infrastructure.Git;

public sealed class PostgresRepositoryLockProvider : IRepositoryLockProvider
{
    private static readonly TimeSpan RetryInterval = TimeSpan.FromMilliseconds(250);
    private readonly string _connectionString;

    public PostgresRepositoryLockProvider(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is required for repository locks.");
    }

    public async Task<IAsyncDisposable> AcquireAsync(
        Guid repositoryId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var bytes = repositoryId.ToByteArray();
        var key1 = BitConverter.ToInt32(bytes, 0);
        var key2 = BitConverter.ToInt32(bytes, 4);
        var deadline = DateTimeOffset.UtcNow.Add(timeout);

        try
        {
            while (DateTimeOffset.UtcNow < deadline)
            {
                await using var command = new NpgsqlCommand(
                    "SELECT pg_try_advisory_lock(@key1, @key2);",
                    connection);
                command.Parameters.AddWithValue("key1", key1);
                command.Parameters.AddWithValue("key2", key2);

                if (await command.ExecuteScalarAsync(cancellationToken) is true)
                {
                    return new AdvisoryLockLease(connection, key1, key2);
                }

                await Task.Delay(RetryInterval, cancellationToken);
            }
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }

        await connection.DisposeAsync();
        throw new TimeoutException("GIT_WORKSPACE_LOCKED: Timed out waiting for the repository workspace lock.");
    }

    private sealed class AdvisoryLockLease : IAsyncDisposable
    {
        private readonly NpgsqlConnection _connection;
        private readonly int _key1;
        private readonly int _key2;
        private bool _disposed;

        public AdvisoryLockLease(NpgsqlConnection connection, int key1, int key2)
        {
            _connection = connection;
            _key1 = key1;
            _key2 = key2;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                await using var command = new NpgsqlCommand(
                    "SELECT pg_advisory_unlock(@key1, @key2);",
                    _connection);
                command.Parameters.AddWithValue("key1", _key1);
                command.Parameters.AddWithValue("key2", _key2);
                await command.ExecuteNonQueryAsync();
            }
            finally
            {
                await _connection.DisposeAsync();
            }
        }
    }
}
