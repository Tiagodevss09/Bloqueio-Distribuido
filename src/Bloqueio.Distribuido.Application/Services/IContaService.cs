using Bloqueio.Distribuido.Domain.Entities;

namespace Bloqueio.Distribuido.Application.Services
{
    public interface IContaService
    {

        public Task< bool> SacarAsync(decimal valor, Conta conta, CancellationToken cancellationToken = default);

    }
}