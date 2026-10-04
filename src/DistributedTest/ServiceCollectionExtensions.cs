namespace DistributedTest;

using DistributedTest.Services;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using System.Text.Json;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfra(IConfiguration configuration)
        {
            services
                 .AddHostedService<CacheModifierBackgroundService>()

                 .AddScoped<ItemsQueryService>()
                 .AddScoped<IDistributedLock, PostgresDistributedLock>();

            services.AddNpgsqlDataSource("", (sp, dataSourceBuilder) =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var connectionString = configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("Postgres connection string is missing.");

                dataSourceBuilder.ConnectionStringBuilder.ConnectionString = connectionString;
            });

            var jsonOptions = new JsonSerializerOptions
            {
                IncludeFields = true
            };

            services
                .AddFusionCache()
                .WithSerializer(new FusionCacheSystemTextJsonSerializer(jsonOptions))
                .WithDistributedCache(new RedisCache(new RedisCacheOptions
                {
                    Configuration = configuration.GetConnectionString("Redis") ?? throw new InvalidOperationException("Redis connection string is missing.")
                }))
                .WithBackplane(new RedisBackplane(new RedisBackplaneOptions
                {
                    Configuration = configuration.GetConnectionString("Redis") ?? throw new InvalidOperationException("Redis connection string is missing.")
                }))
                .AsHybridCache();

            return services;
        }
    }
}
