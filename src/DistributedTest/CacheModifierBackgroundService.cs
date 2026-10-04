namespace DistributedTest;

using DistributedTest.Services;
using Microsoft.Extensions.Caching.Hybrid;
using Npgsql;

public sealed class CacheModifierBackgroundService : BackgroundService
{
    private const string AdvisoryLockNamespace = "DistributedTest";
    private const string CacheKey = "test";

    private readonly ILogger<CacheModifierBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NpgsqlDataSource _dataSource;
    private readonly HybridCache _cache;

    public CacheModifierBackgroundService(ILogger<CacheModifierBackgroundService> logger, IServiceScopeFactory scopeFactory, NpgsqlDataSource dataSource, HybridCache cache)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _dataSource = dataSource;
        _cache = cache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

        try
        {
            do
            {
                try
                {
                    await TryModifyDataAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while modifying cached data.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Clean shutdown.
        }
    }

    private async Task TryModifyDataAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var distributedLock = scope.ServiceProvider.GetRequiredService<IDistributedLock>();

        await using var lockHandle = await distributedLock.TryAcquireAsync(AdvisoryLockNamespace, cancellationToken);

        if (lockHandle is null)
        {
            _logger.LogInformation("Another instance currently owns distributed lock {LockNamespace}.", AdvisoryLockNamespace);

            return;
        }

        _logger.LogInformation("Acquired distributed lock {LockNamespace}.", AdvisoryLockNamespace);

        await InsertRandomDataAsync(cancellationToken);

        await _cache.RemoveAsync(CacheKey, cancellationToken);

        _logger.LogInformation("Random data inserted and cache key {CacheKey} invalidated.", CacheKey);
    }

    private async Task InsertRandomDataAsync(CancellationToken cancellationToken)
    {
        var value = Guid.NewGuid().ToString();
        var number = Random.Shared.Next(1, 10_000);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO "TestData" ("Value", "Number", "CreatedAt")
            VALUES (@value, @number, NOW());
            """;

        command.Parameters.AddWithValue("value", value);
        command.Parameters.AddWithValue("number", number);

        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation("Inserted random data. Value: {Value}, Number: {Number}.", value, number);
    }
}