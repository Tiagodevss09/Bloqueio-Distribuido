
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Medallion.Threading.Redis;
using StackExchange.Redis;

namespace Bloqueio.Distribuido;

public static class RedisCacheExtensions
{
    public static IServiceCollection AddRedisCacheWithLock( this IServiceCollection services, Action<CacheRedisOptions> configuration)
    {
        services.AddOptions<CacheRedisOptions>()
            .Configure(configuration)
            .Validate( options => !string.IsNullOrWhiteSpace(options.Host),"O host do Redis é obrigatório.")
            .ValidateOnStart();

        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<CacheRedisOptions>>()
                .Value;

            return ConnectionMultiplexer.Connect(
                options.GetConnectionString());
        });

        services.AddSingleton<ICacheService, CacheService>();
        services.AddSingleton<IDistributedLockService, DistributedLockService>();

        return services;
    }
}
