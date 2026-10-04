namespace DistributedTest.Services;

using Npgsql;

public sealed class PostgresDistributedLock : IDistributedLock
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresDistributedLock(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<IAsyncDisposable?> TryAcquireAsync(string lockNamespace, CancellationToken cancellationToken)
    {
        var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """SELECT pg_try_advisory_xact_lock(hashtext(@namespace)::bigint);""";
        command.Parameters.AddWithValue("namespace", lockNamespace);

        var acquired = (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
        if (!acquired)
        {
            await transaction.RollbackAsync(cancellationToken);
            await transaction.DisposeAsync();
            await connection.DisposeAsync();

            return null;
        }

        return new PostgresDistributedLockHandle(connection, transaction);
    }

    private sealed class PostgresDistributedLockHandle : IAsyncDisposable
    {
        private readonly NpgsqlConnection _connection;
        private readonly NpgsqlTransaction _transaction;

        public PostgresDistributedLockHandle(NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            _connection = connection;
            _transaction = transaction;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                await _connection.DisposeAsync();
            }
        }
    }
}