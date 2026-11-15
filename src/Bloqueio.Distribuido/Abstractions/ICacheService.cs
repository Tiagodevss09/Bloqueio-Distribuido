using StackExchange.Redis;

namespace Bloqueio.Distribuido
{
    public interface ICacheService
    {
        IDatabase Database { get; }
    }
}
