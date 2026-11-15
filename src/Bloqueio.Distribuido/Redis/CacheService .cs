using Bloqueio.Distribuido;
using StackExchange.Redis;

public sealed class CacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;

    public CacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public IDatabase Database => _redis.GetDatabase();
}