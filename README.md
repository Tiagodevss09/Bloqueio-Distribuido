# Bloqueio.Distribuido

Biblioteca .NET para gerenciamento de **locks distribuídos utilizando Redis**, permitindo controlar o acesso concorrente a recursos compartilhados em aplicações distribuídas.

## Sobre o projeto

O `Bloqueio.Distribuido` é uma biblioteca desenvolvida em C# com o objetivo de disponibilizar uma abstração simples e reutilizável para utilização de locks distribuídos em aplicações .NET.

Em ambientes distribuídos, diferentes instâncias de uma aplicação podem executar operações simultaneamente sobre um mesmo recurso. Mecanismos de sincronização locais, como `lock` ou `SemaphoreSlim`, não são suficientes para coordenar o acesso entre diferentes processos ou servidores.

Para solucionar esse cenário, o projeto utiliza o Redis como mecanismo compartilhado de coordenação e a biblioteca Medallion.Threading para gerenciamento dos locks distribuídos.

## Objetivos

- Centralizar o gerenciamento de locks distribuídos.
- Controlar o acesso concorrente a recursos compartilhados.
- Permitir a configuração do tempo de expiração do lock.
- Definir um tempo máximo de espera para aquisição do lock.
- Facilitar a integração com aplicações .NET por meio de injeção de dependência.
- Reutilizar uma conexão Redis por meio de `IConnectionMultiplexer`.

## Tecnologias utilizadas

- C#
- .NET 10
- Redis
- StackExchange.Redis
- Medallion.Threading
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Options

## Componentes

| Componente | Responsabilidade |
|---|---|
| `ICacheService` | Disponibiliza acesso ao banco Redis. |
| `IDistributedLockService` | Define o contrato para aquisição de locks distribuídos. |
| `DistributedLockService` | Implementa o gerenciamento dos locks distribuídos. |
| `CacheService` | Encapsula o acesso ao `IDatabase`. |
| `RedisCacheExtensions` | Centraliza o registro das dependências. |
| `CacheRedisOptions` | Define as configurações de conexão Redis. |

## Configuração

### 1. Redis

O projeto utiliza o Redis como infraestrutura compartilhada para coordenar a aquisição e liberação dos locks entre diferentes instâncias da aplicação.

Exemplo de configuração no `appsettings.json`:

```json
{
  "CacheRedis": {
    "Host": "localhost:6379",
    "Password": "",
    "Format": "{0},password={1},abortConnect=false"
  }
}
```

A propriedade `Format` define o formato da connection string utilizada pelo StackExchange.Redis.

### 2. Registro dos serviços

No `Program.cs`, registre os serviços da biblioteca:

```csharp
using Bloqueio.Distribuido;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRedisCacheWithLock(options =>
{
    builder.Configuration
        .GetSection("CacheRedis")
        .Bind(options);
});

var app = builder.Build();

app.Run();
```

A extensão `AddRedisCacheWithLock` realiza o registro das dependências necessárias para utilização do Redis e do serviço de lock distribuído.

## Utilização

### 1. Injeção de dependência

Injete a abstração `IDistributedLockService` na classe que precisa controlar o acesso concorrente:

```csharp
public class ContaService
{
    private readonly IDistributedLockService _lockService;

    public ContaService(IDistributedLockService lockService)
    {
        _lockService = lockService;
    }
}
```

### 2. Aquisição do lock

Exemplo de utilização em uma operação de saque:

```csharp
public async Task SacarAsync(
    Guid contaId,
    decimal valor,
    CancellationToken cancellationToken)
{
    var lockName = $"conta:{contaId}:saque";

    var expiration = TimeSpan.FromMinutes(5);
    var timeout = TimeSpan.FromSeconds(10);

    await using var handle = await _lockService.AcquireAsync(
        lockName,
        expiration,
        timeout,
        cancellationToken);

    if (handle is null)
    {
        throw new InvalidOperationException(
            "Não foi possível adquirir o lock da conta.");
    }

    // Executar operação protegida pelo lock.
}
```

### 3. Parâmetros do método

O método `AcquireAsync` recebe os seguintes parâmetros:

| Parâmetro | Descrição |
|---|---|
| `lockName` | Identificador único do recurso que será protegido. |
| `expiration` | Tempo de expiração configurado para o lock. |
| `timeout` | Tempo máximo de espera pela aquisição do lock. |
| `cancellationToken` | Permite cancelar a operação de aquisição. |

**Retorno:**

- `IAsyncDisposable`: indica que o lock foi adquirido. Seu descarte realiza a liberação do recurso.
- `null`: indica que não foi possível adquirir o lock dentro do tempo máximo de espera.

## Como funciona o lock distribuído?

Considere duas instâncias de uma API que precisam executar uma operação sobre a mesma conta bancária.

```text
                  Redis
                    |
          Lock: conta:123:saque
                    |
          +---------+---------+
          |                   |
        API 01              API 02
          |                   |
     Solicita lock       Solicita lock
          |                   |
     Lock adquirido      Aguarda aquisição
          |                   |
     Executa saque       Aguarda liberação
          |                   |
     Libera lock               |
          |                   |
          +--------- Lock adquirido
                              |
                         Executa saque
                              |
                         Libera lock
```

O Redis atua como ponto compartilhado de coordenação, permitindo que diferentes instâncias da aplicação utilizem o mesmo identificador de lock.

Dessa forma, enquanto uma instância mantém o bloqueio de determinado recurso, as demais precisam aguardar sua liberação ou atingir o tempo máximo de espera configurado.

## Considerações importantes

- O identificador do lock deve representar o recurso que precisa ser protegido.
- Todas as instâncias que precisam coordenar o acesso devem utilizar o mesmo Redis e o mesmo identificador de lock.
- O objeto retornado pela aquisição deve ser descartado para liberar o lock.
- O tempo de espera (`timeout`) e o tempo de expiração (`expiration`) possuem finalidades diferentes.
- O lock distribuído não substitui transações, constraints ou mecanismos de integridade do banco de dados.
- Operações críticas, especialmente financeiras, devem manter suas garantias de consistência no banco de dados.
- A disponibilidade e a configuração do Redis são fundamentais para o funcionamento do mecanismo.
