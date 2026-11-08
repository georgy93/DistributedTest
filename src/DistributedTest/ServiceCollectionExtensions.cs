namespace DistributedTest;

using Microsoft.Extensions.Caching.StackExchangeRedis;
using Services;
using ServiceStack.Redis;
using ServiceStack.Text;
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


            //var redisClient = new RedisClient(configuration.GetConnectionString("Redis"));

            ////Initialize and set counter to '1'
            //redisClient.Increment("atomic-counter", 1);

            ////Acquire lock and never release it
            //redisClient.AcquireLock("testlock");

            //var waitFor = TimeSpan.FromSeconds(2);
            //var now = DateTime.Now;

            //using (var newClient = new RedisClient(configuration.GetConnectionString("Redis")))
            //{
            //    //Attempt to acquire a lock with a 2 second timeout
            //    using (newClient.AcquireLock("testlock", waitFor))
            //    {
            //        //If lock was acquired this would be incremented to '2'
            //        redisClient.Increment("atomic-counter", 1);
            //    }
            //}

            return services;
        }
    }
}
