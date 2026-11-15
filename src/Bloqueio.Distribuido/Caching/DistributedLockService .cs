
using Bloqueio.Distribuido.Application.Exceptions;
using Medallion.Threading.Redis;
using StackExchange.Redis;

namespace Bloqueio.Distribuido;

public sealed class DistributedLockService : IDistributedLockService
{
    private readonly IConnectionMultiplexer _redis;

    public DistributedLockService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<IAsyncDisposable?> AcquireAsync(string lockName,TimeSpan expiration, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var distributedLock = new RedisDistributedLock(lockName,_redis.GetDatabase(), options => options.Expiry(expiration));
        try
        {
            return await distributedLock.AcquireAsync(timeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            //return null;
            throw new DistributedLockUnavailableException("Não foi possível adquirir o bloqueio da conta.");
        }
        catch (Exception ex)
        {
            throw new DistributedLockUnavailableException(ex.Message);
        }
    }
    
}
