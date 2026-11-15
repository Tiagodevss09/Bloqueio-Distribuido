namespace Bloqueio.Distribuido.Application.Exceptions
{
    public sealed class DistributedLockUnavailableException(string message) : Exception(message);
}
