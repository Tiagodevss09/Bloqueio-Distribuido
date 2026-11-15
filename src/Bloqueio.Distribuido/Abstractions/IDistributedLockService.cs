namespace Bloqueio.Distribuido
{
    public interface IDistributedLockService
    {
        Task<IAsyncDisposable?> AcquireAsync(string lockName,TimeSpan expiration, TimeSpan timeout,CancellationToken cancellationToken = default);
    }
}
