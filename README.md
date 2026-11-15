# \# Bloqueio.Distribuido

# 

# Biblioteca .NET para gerenciamento de \*\*locks distribuídos utilizando Redis\*\*, permitindo controlar o acesso concorrente a recursos compartilhados em aplicações distribuídas.

# 

# \## Sobre o projeto

# 

# O `Bloqueio.Distribuido` é uma biblioteca desenvolvida em C# com o objetivo de disponibilizar uma abstração simples para utilização de locks distribuídos em aplicações .NET.

# 

# Em ambientes distribuídos, diferentes instâncias de uma aplicação podem executar operações simultaneamente sobre um mesmo recurso. Um mecanismo de sincronização local, como `lock` ou `SemaphoreSlim`, não é suficiente para coordenar o acesso entre diferentes processos ou servidores.

# 

# Para resolver esse cenário, o projeto utiliza o Redis como mecanismo compartilhado de coordenação e a biblioteca Medallion.Threading para gerenciamento dos locks.

# 

# \## Objetivos

# 

# \- Centralizar o gerenciamento de locks distribuídos.

# \- Controlar o acesso concorrente a recursos compartilhados.

# \- Permitir configuração do tempo de expiração do lock.

# \- Definir um tempo máximo de espera para aquisição do lock.

# \- Facilitar a integração com aplicações .NET por meio de injeção de dependência.

# \- Reutilizar uma conexão Redis por meio de `IConnectionMultiplexer`.

# 

# \## Tecnologias utilizadas

# 

# \- C#

# \- .NET

# \- Redis

# \- StackExchange.Redis

# \- Medallion.Threading

# \- Microsoft.Extensions.DependencyInjection

# \- Microsoft.Extensions.Options

# 

# \### Componentes

# 

# | Componente | Responsabilidade |

# |---|---|

# | `ICacheService` | Disponibiliza acesso ao banco Redis |

# | `IDistributedLockService` | Define o contrato para aquisição de locks |

# | `DistributedLockService` | Implementa o gerenciamento de locks distribuídos |

# | `CacheService` | Encapsula o acesso ao `IDatabase` |

# | `RedisCacheExtensions` | Centraliza o registro das dependências |

# | `CacheRedisOptions` | Define as configurações de conexão Redis |

# 

# \## Configuração

# 

# \### 1. Redis

# 

# O projeto utiliza o Redis como infraestrutura compartilhada para coordenar os locks entre as instâncias da aplicação.

# 

# Exemplo de configuração no `appsettings.json`:

# 

# ```json

# {

# &#x20; "CacheRedis": {

# &#x20;   "Host": "localhost:6379",

# &#x20;   "Password": "",

# &#x20;   "Format": "{0},password={1},abortConnect=false"

# &#x20; }

# }

# ```

# 

# A propriedade `Format` define o formato da connection string utilizada pelo StackExchange.Redis.

# 

# \### 2. Registro dos serviços

# 

# No `Program.cs`, registre os serviços da biblioteca:

# 

# ```csharp

# using Bloqueio.Distribuido;

# 

# var builder = WebApplication.CreateBuilder(args);

# 

# builder.Services.AddRedisCacheWithLock(options =>

# {

# &#x20;   builder.Configuration

# &#x20;       .GetSection("CacheRedis")

# &#x20;       .Bind(options);

# });

# 

# var app = builder.Build();

# 

# app.Run();

# ```

# 

# A extensão `AddRedisCacheWithLock` realiza o registro das dependências necessárias para utilização do Redis e do serviço de lock distribuído.

# 

# \## Utilização

# 

# \### 1. Injeção de dependência

# 

# Injete a abstração `IDistributedLockService` na classe que precisa controlar o acesso concorrente:

# 

# ```csharp

# public class ContaService

# {

# &#x20;   private readonly IDistributedLockService \_lockService;

# 

# &#x20;   public ContaService(IDistributedLockService lockService)

# &#x20;   {

# &#x20;       \_lockService = lockService;

# &#x20;   }

# }

# ```

# 

# \### 2. Aquisição do lock

# 

# Exemplo de utilização em uma operação de saque:

# 

# ```csharp

# public async Task SacarAsync(

# &#x20;   Guid contaId,

# &#x20;   decimal valor,

# &#x20;   CancellationToken cancellationToken)

# {

# &#x20;   var lockName = $"conta:{contaId}:saque";

# 

# &#x20;   var expiration = TimeSpan.FromMinutes(5);

# &#x20;   var timeout = TimeSpan.FromSeconds(10);

# 

# &#x20;   await using var handle = await \_lockService.AcquireAsync(

# &#x20;       lockName,

# &#x20;       expiration,

# &#x20;       timeout,

# &#x20;       cancellationToken);

# 

# &#x20;   if (handle is null)

# &#x20;   {

# &#x20;       throw new InvalidOperationException(

# &#x20;           "Não foi possível adquirir o lock da conta.");

# &#x20;   }

# 

# &#x20;   // Executar operação protegida pelo lock.

# }

# ```

# 

# \### 3. Parâmetros do método

# 

# O método `AcquireAsync` recebe os seguintes parâmetros:

# 

# | Parâmetro | Descrição |

# |---|---|

# | `lockName` | Identificador único do recurso que será protegido |

# | `expiration` | Tempo de expiração configurado para o lock |

# | `timeout` | Tempo máximo de espera pela aquisição |

# | `cancellationToken` | Permite cancelar a operação |

# 

# \*\*Retorno:\*\*

# 

# \- `IAsyncDisposable`: indica que o lock foi adquirido. O descarte libera o recurso.

# \- `null`: indica que o tempo máximo de espera foi atingido e o lock não foi adquirido.

# 

# \## Como funciona o lock distribuído?

# 

# Considere duas instâncias de uma API que precisam executar uma operação sobre a mesma conta bancária.

# 

# ```text

# &#x20;                Redis

# &#x20;                  |

# &#x20;         Lock: conta:123:saque

# &#x20;                  |

# &#x20;         +--------+--------+

# &#x20;         |                 |

# &#x20;      API 01             API 02

# &#x20;         |                 |

# &#x20;    Solicita lock     Solicita lock

# &#x20;         |                 |

# &#x20;    Lock adquirido    Aguarda aquisição

# &#x20;         |                 |

# &#x20;    Executa saque     Aguarda liberação

# &#x20;         |                 |

# &#x20;    Libera lock             |

# &#x20;         |                 |

# &#x20;         +---------- Lock adquirido

# &#x20;                           |

# &#x20;                      Executa saque

# &#x20;                           |

# &#x20;                      Libera lock

# ```

# 

# O Redis atua como ponto compartilhado de coordenação, permitindo que diferentes instâncias da aplicação utilizem a mesma chave de lock.

# 

# \## Considerações importantes

# 

# \- O identificador do lock deve representar o recurso que precisa ser protegido.

# \- Todas as instâncias que precisam coordenar o acesso devem utilizar o mesmo Redis e o mesmo identificador de lock.

# \- O objeto retornado pela aquisição deve ser descartado para liberar o lock.

# \- O tempo de espera (`timeout`) e o tempo de expiração (`expiration`) possuem finalidades diferentes.

# \- O lock distribuído não substitui transações, constraints ou mecanismos de integridade do banco de dados.

# \- Operações críticas, especialmente financeiras, devem manter suas garantias de consistência no banco de dados.

# \- A disponibilidade e a configuração do Redis são fundamentais para o funcionamento do mecanismo.


