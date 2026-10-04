namespace DistributedTest.Services;

using Dapper;
using Microsoft.Extensions.Caching.Hybrid;
using Npgsql;

public class ItemsQueryService
{
    private readonly HybridCache _hybridCache;
    private readonly NpgsqlDataSource _dataSource;
    private readonly HybridCacheEntryOptions _options = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(5)
    };

    public ItemsQueryService(HybridCache hybridCache, NpgsqlDataSource dataSource)
    {
        _hybridCache = hybridCache;
        _dataSource = dataSource;
    }

    public async Task<IEnumerable<TestData>> GetItemsAsync(CancellationToken cancellationToken) => await _hybridCache.GetOrCreateAsync("test", async ct =>
    {
        await using var connection = await _dataSource.OpenConnectionAsync(ct);

        const string sql =
            """
                SELECT "Id", "Value", "Number", "CreatedAt"
                FROM "TestData"
                ORDER BY "Id" DESC;
            """;

        return await connection.QueryAsync<TestData>(new CommandDefinition(sql, cancellationToken: ct));
    },
    _options, cancellationToken: cancellationToken);
}
