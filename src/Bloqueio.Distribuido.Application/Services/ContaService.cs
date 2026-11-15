using Bloqueio.Distribuido.Application.Exceptions;
using Bloqueio.Distribuido.Domain.Entities;

namespace Bloqueio.Distribuido.Application.Services
{
    public class ContaService : IContaService
    {
        private readonly IDistributedLockService _lockService;

        public ContaService(IDistributedLockService lockService)
        {
            _lockService = lockService;
        }
        public async Task<bool> SacarAsync(decimal valor, Conta conta, CancellationToken cancellationToken = default)
        {
            await using var handle = await _lockService.AcquireAsync(
                lockName: $"conta:{conta.Numero}:saque",
                expiration: TimeSpan.FromMinutes(1),
                timeout: TimeSpan.FromSeconds(10),
                cancellationToken: cancellationToken);

            //simula um processo de 20 segundos
            Thread.Sleep(TimeSpan.FromSeconds(20));

            //if (handle is null)
            //{
            //    throw new DistributedLockUnavailableException("Não foi possível adquirir o bloqueio da conta.");
            //}


            conta.Sacar(valor);

            return true;
        }
    }
}
