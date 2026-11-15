using Bloqueio.Distribuido.Application.Services;
using Bloqueio.Distribuido.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Bloqueio.Distribuido.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContaController : ControllerBase
{
    private readonly IContaService _lockService;
    private readonly ILogger<ContaController> _logger;

    public ContaController(IContaService lockService,ILogger<ContaController> logger)
    {
        _lockService = lockService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return Ok("API de bloqueio distribuído funcionando.");
    }

    [HttpPost("sacar")]
    public async Task<IActionResult> Sacar([FromQuery] int contaId,[FromQuery] decimal valor, Conta conta,  CancellationToken cancellationToken)
    {
        if (valor <= 0)
        {
            return BadRequest("O valor deve ser maior que zero.");
        }

        var saldoAntes = conta.ValorDisponivel();
        LogarSaldoPreSaque(contaId, valor, saldoAntes);

        var result = await _lockService.SacarAsync(valor, conta, cancellationToken);  
        var saldoDepois = conta.ValorDisponivel();
     
        LogarSaldoPosSaque(contaId, valor, saldoAntes, saldoDepois, result);

        return Ok("Saque processado.");
    }

    private void LogarSaldoPreSaque(int contaId, decimal valor,decimal saldoAtual)
    {
        _logger.LogInformation("Iniciando saque. Conta: {ContaId} | Valor: {Valor} | Saldo anterior: {SaldoAntes}",
            contaId,
            valor,
            saldoAtual);
    }

    private void LogarSaldoPosSaque(int contaId,decimal valor,decimal saldoAntes,decimal saldoDepois,bool resultado)
    {
        _logger.LogInformation(
            "Saque finalizado. Conta: {ContaId} | Valor: {Valor} | Saldo anterior: {SaldoAntes} | Saldo atual: {SaldoDepois} | Resultado: {Resultado}",
            contaId,
            valor,
            saldoAntes,
            saldoDepois,
            resultado);
    }
}
