# Camada Audit — ArchChallenge.CashFlow.Infrastructure.CrossCutting.Audit

A camada de auditoria implementa um rastro imutável e criptograficamente verificável de todas as operações de negócio sobre agregados marcados como auditáveis. O mecanismo combina três peças ortogonais: **pipeline de auditoria** (Application), **Outbox transacional** (Relational) e **persistência imutável** (ImmuDB).

---

## Responsabilidades

- Capturar, atomicamente à escrita do agregado, um registro de auditoria no Outbox de auditoria (`TB_AUDIT_OUTBOX_EVENT`) dentro da mesma transação PostgreSQL.
- Enriquecer cada comando com `UserId` (claim JWT `sub`) e `OccurredAt` antes que o handler seja executado.
- Propagar os metadados de auditoria pelo pipeline MediatR sem que os handlers precisem conhecer o mecanismo.
- Gravar os registros de auditoria no **ImmuDB** — banco de dados append-only com verificação criptográfica — garantindo não-repúdio e imutabilidade.
- Prover health check do ImmuDB integrado ao readiness (`/health/readiness`, tag `ready`).

---

## Visão das peças e seus projetos


| Peça                                               | Projeto / Namespace                             | Tipo                                                                                                                                                                                                                                                                                                                                |
| -------------------------------------------------- | ----------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `IAuditableCommand`                                | `Infrastructure.CrossCutting.Audit.Interfaces`  | Interface de contrato (com `UserId`/`OccurredAt`)                                                                                                                                                                                                                                                                                   |
| `CommandBase`                                      | `Application.Common.Commands`                   | Record abstrato — implementa só `IAuditableCommand`; **não** herda `MediatR.IRequest` (evita conflito com `IRequest<EnqueueResult>`). Cada comando declara `IRequest` / `IRequest<T>` explicitamente (ex.: `ExecuteTransactionCommand : IRequest`; `EnqueueTransactionCommand` via `IEnqueueCommand<>` → `IRequest<EnqueueResult>`) |
| `IAuditContext`                                    | `Domain.Shared.Audit`                           | Interface (contrato de escopo scoped)                                                                                                                                                                                                                                                                                               |
| `AuditBehavior<TRequest,TResponse>`                | `Application.Common.Behaviors`                  | Pipeline Behavior MediatR                                                                                                                                                                                                                                                                                                           |
| `IdentityCommandFilter`                            | `Infrastructure.CrossCutting.Security.Filters`  | Action Filter ASP.NET Core — extrai identidade JWT                                                                                                                                                                                                                                                                                  |
| `AuditContext`                                     | `Infrastructure.CrossCutting.Audit.Contexts`    | Implementação scoped de `IAuditContext`                                                                                                                                                                                                                                                                                             |
| `IImmutableAuditWriter`                            | `Infrastructure.CrossCutting.Audit.Interfaces`  | Interface do adaptador ImmuDB                                                                                                                                                                                                                                                                                                       |
| `ImmuDbAuditWriter`                                | `Infrastructure.CrossCutting.Audit.Database`    | Adaptador ImmuDB (singleton)                                                                                                                                                                                                                                                                                                        |
| `ImmuDbOptions`                                    | `Infrastructure.CrossCutting.Audit.Database`    | Configuração ImmuDB                                                                                                                                                                                                                                                                                                                 |
| `ImmuDbHealthCheck`                                | `Infrastructure.CrossCutting.Audit.Healthcheck` | Health check do ImmuDB                                                                                                                                                                                                                                                                                                              |
| `AuditOutboxEvent`                                 | `Domain.Shared.Events`                          | Entidade (tabela `TB_AUDIT_OUTBOX_EVENT`)                                                                                                                                                                                                                                                                                           |
| `IAuditOutboxRepository` / `AuditOutboxRepository` | `Relational`                                    | Repositório de auditoria                                                                                                                                                                                                                                                                                                            |
| `AuditOutboxWorkerService`                         | `Relational`                                    | Background service de polling                                                                                                                                                                                                                                                                                                       |
| `UnitOfWork`                                       | `Relational`                                    | Materialização do outbox de auditoria                                                                                                                                                                                                                                                                                               |


---

## Fluxo completo de auditoria

O fluxo atravessa cinco estágios ordenados, todos dentro do ciclo de vida de uma única requisição e de um background worker posterior:

```mermaid
flowchart TD
    A[1. HTTP Request chega com JWT] --> B[IdentityCommandFilter\nextrai sub do JWT\npreenche UserId e OccurredAt via IAuditableCommand]
    B --> C[AuditBehavior pipeline\nauditContext.SetMetadata]
    C --> D[ExecuteTransactionHandler\nauditContext.Capture de Transaction]
    D --> E[UnitOfWork.SaveChangesAsync\nTryBuildAuditOutboxPayload\nAddAsync AuditOutboxEvent\nNotifyPersisted]
    E --> F[PostgreSQL\nTB_AUDIT_OUTBOX_EVENT\natômico com TB_TRANSACTION]
    F --> G[AuditOutboxWorkerService\npolling batch]
    G --> H[ImmuDbAuditWriter\nVerifiedSet audit:uuid]
    H --> I[ImmuDB\nregistro imutável\ncriptograficamente verificável]
```



> **Boundary assíncrono:** `UserId` e `OccurredAt` são propagados além da requisição HTTP via `EnqueueTransactionMessage` (que carrega esses campos). O `ExecuteTransactionConsumer` reconstrói o `ExecuteTransactionCommand` com os mesmos valores, garantindo rastreabilidade mesmo quando o handler executa em um worker separado.

---

## Diagrama de Classes — Contratos e base de comandos

As peças de contrato estão distribuídas em três projetos distintos após a reorganização:

- `IAuditableCommand` → `Infrastructure.CrossCutting.Audit.Interfaces`
- `CommandBase` → `Application.Common.Commands`
- `IAuditContext`, `AuditOutboxEvent`, `IAuditOutboxRepository` → `Domain.Shared`

```mermaid
classDiagram
    direction TB

    class IAuditableCommand {
        <<interface>>
        +string UserId
        +DateTime OccurredAt
    }

    class CommandBase {
        <<abstract record>>
        +string UserId
        +DateTime OccurredAt
        note: Application.Common.Commands\nIAuditableCommand; IRequest por comando concreto
    }

    class IAuditContext {
        <<interface>>
        +SetMetadata(userId, occurredAt, action) void
        +Capture(IAggregateRoot) void
        +TryBuildAuditOutboxPayload(out eventType, out payloadJson) bool
        +NotifyPersisted() void
    }

    class AuditOutboxEvent {
        +string EventType
        +string Payload
        +bool Processed
        +DateTime? ProcessedAt
        +int RetryCount
        +MarkProcessed() void
        +IncrementRetry() void
    }

    class IAuditOutboxRepository {
        <<interface>>
        +GetPendingAsync(batchSize, CT) IReadOnlyList~AuditOutboxEvent~
        +SaveChangesAsync(CT) Task
    }

    class Entity {
        <<abstract>>
    }

    class ExecuteTransactionCommand {
        +Guid TaskId
        +TransactionType Type
        +decimal Amount
        +string? Description
    }

    AuditOutboxEvent --|> Entity
    CommandBase ..|> IAuditableCommand : implementa
    ExecuteTransactionCommand --|> CommandBase : herda
```



---

## Diagrama de Classes — Infraestrutura (Infrastructure.CrossCutting.Audit)

```mermaid
classDiagram
    direction TB

    class AuditContext {
        -string? _userId
        -DateTime _occurredAt
        -string? _action
        -IAggregateRoot? _aggregate
        -bool _persisted
        +SetMetadata(userId, occurredAt, action) void
        +Capture(IAggregateRoot) void
        +TryBuildAuditOutboxPayload(out string, out string) bool
        +NotifyPersisted() void
    }

    class IImmutableAuditWriter {
        <<interface>>
        +WritePayloadAsync(key, jsonPayload, CT) Task
    }

    class ImmuDbAuditWriter {
        -ImmuClient? _client
        -SemaphoreSlim _init
        -SemaphoreSlim _write
        +WritePayloadAsync(key, jsonPayload, CT) Task
        -EnsureSessionAsync(CT) Task
        +DisposeAsync() ValueTask
    }

    class ImmuDbOptions {
        +string Host
        +int Port
        +string Username
        +string Password
        +string Database
    }

    class ImmuDbHealthCheck {
        +CheckHealthAsync(context, CT) HealthCheckResult
    }

    class AuditBehavior~TRequest, TResponse~ {
        +Handle(request, next, CT) Task~TResponse~
    }

    class AuditOutboxWorkerService {
        -AuditWorkerOptions _options
        #ExecuteAsync(CT) Task
        -ProcessPendingAsync(CT) Task
        -ProcessSingleAsync(AuditOutboxEvent, CT) Task
    }

    class AuditWorkerOptions {
        +int PollingIntervalSeconds
        +int BatchSize
        +int MaxRetries
    }

    AuditContext ..|> IAuditContext
    ImmuDbAuditWriter ..|> IImmutableAuditWriter
    ImmuDbAuditWriter --> ImmuDbOptions : configuração
    ImmuDbHealthCheck --> IImmutableAuditWriter
    AuditOutboxWorkerService --> IAuditOutboxRepository
    AuditOutboxWorkerService --> IImmutableAuditWriter
    AuditOutboxWorkerService --> AuditWorkerOptions
    AuditBehavior --> IAuditContext
```



---

## Diagrama de Sequência — Captura de auditoria na requisição HTTP

Este diagrama detalha como os metadados de auditoria são coletados e persistidos atomicamente durante o processamento de `ExecuteTransactionCommand`.

```mermaid
sequenceDiagram
    autonumber
    participant Req as HTTP Request (JWT)
    participant ICF as IdentityCommandFilter
    participant AB as AuditBehavior
    participant AC as AuditContext (scoped)
    participant H as ExecuteTransactionHandler
    participant UoW as UnitOfWork
    participant PG as PostgreSQL

    Req->>ICF: OnActionExecutionAsync
    ICF->>ICF: extrai claim "sub" → UserId, DateTime.UtcNow → OccurredAt
    ICF->>ICF: itera ActionArguments.OfType~IAuditableCommand~
    ICF->>ICF: arg.UserId = userId; arg.OccurredAt = occurredAt

    ICF->>AB: pipeline MediatR — AuditBehavior executa
    AB->>AB: request is IAuditableCommand?
    alt é auditável (todos CommandBase)
        AB->>AC: SetMetadata(cmd.UserId, cmd.OccurredAt, "ExecuteTransactionCommand")
    end
    AB->>H: next() → handler executa

    H->>H: new Transaction(...)
    H->>AC: Capture(entity)
    H->>UoW: SaveChangesAsync

    UoW->>AC: TryBuildAuditOutboxPayload(out eventType, out payload)
    AC-->>UoW: true + payload JSON {userId, occurredAt, action, aggregateType, aggregateId, state}
    UoW->>PG: AddAsync(AuditOutboxEvent) + SaveChangesAsync
    Note over PG: Atômico com a escrita do Transaction
    UoW->>AC: NotifyPersisted() — limpa estado scoped
```



---

## Diagrama de Sequência — Propagação de identidade pelo boundary assíncrono

`UserId` e `OccurredAt` precisam cruzar o boundary HTTP → RabbitMQ → Consumer. Para isso, `EnqueueTransactionMessage` carrega esses campos e o consumer os reconstrói no comando.

```mermaid
sequenceDiagram
    autonumber
    participant ICF as IdentityCommandFilter
    participant ETC as EnqueueTransactionCommand
    participant EH as EnqueueCommandHandler
    participant EB as IEventBus / RabbitMQ
    participant EC as ExecuteTransactionConsumer
    participant CMD as ExecuteTransactionCommand
    participant AB as AuditBehavior

    ICF->>ETC: UserId = sub, OccurredAt = now (via IAuditableCommand)
    ETC->>EH: EnqueueTransactionCommand{UserId, OccurredAt, ...}
    EH->>EH: BuildMessage(taskId) → EnqueueTransactionMessage{TaskId, UserId, OccurredAt, Type, Amount, ...}
    EH->>EB: PublishAsync(EnqueueTransactionMessage)

    EB->>EC: Consume(EnqueueTransactionMessage)
    EC->>CMD: new ExecuteTransactionCommand{TaskId, Type, ...} with UserId=msg.UserId, OccurredAt=msg.OccurredAt
    CMD->>AB: pipeline MediatR — IAuditableCommand.UserId e OccurredAt já preenchidos
    AB->>AB: SetMetadata(cmd.UserId, cmd.OccurredAt, "ExecuteTransactionCommand")
```



---

## Diagrama de Sequência — Worker de auditoria (AuditOutboxWorkerService)

```mermaid
sequenceDiagram
    autonumber
    participant W as AuditOutboxWorkerService
    participant R as AuditOutboxRepository
    participant PG as PostgreSQL
    participant IW as ImmuDbAuditWriter
    participant IM as ImmuDB

    loop a cada AuditWorkerOptions.PollingIntervalSeconds
        W->>R: GetPendingAsync(batchSize)
        R->>PG: SELECT TB_AUDIT_OUTBOX_EVENT WHERE Processed=false AND RetryCount < 5
        PG-->>R: AuditOutboxEvent[]
        R-->>W: pending rows

        loop para cada AuditOutboxEvent
            W->>IW: WritePayloadAsync("audit:{uuid}", payload)
            IW->>IW: EnsureSessionAsync (lazy open ImmuDB)
            IW->>IM: VerifiedSet(key, value)
            alt sucesso
                IM-->>IW: ok
                W->>W: row.MarkProcessed()
            else falha
                W->>W: row.IncrementRetry()
            end
        end

        W->>R: SaveChangesAsync
        R->>PG: UPDATE TB_AUDIT_OUTBOX_EVENT
    end
```



---

## Diagrama de Sequência — Health Check do ImmuDB

```mermaid
sequenceDiagram
    autonumber
    participant HC as HealthChecks Middleware
    participant IHC as ImmuDbHealthCheck
    participant IW as ImmuDbAuditWriter
    participant IM as ImmuDB

    HC->>IHC: CheckHealthAsync
    IHC->>IW: WritePayloadAsync("health:{ticks}", '{"probe":true}')
    IW->>IM: VerifiedSet
    alt ImmuDB disponível
        IM-->>IHC: ok
        IHC-->>HC: HealthCheckResult.Healthy
    else ImmuDB indisponível
        IM-->>IHC: exception
        IHC-->>HC: HealthCheckResult.Unhealthy
    end
```



---

## Payload de auditoria

Cada entrada gravada no ImmuDB segue o formato JSON abaixo, serializado em `AuditContext.TryBuildAuditOutboxPayload`:

```json
{
  "userId": "sub-claim-do-jwt",
  "occurredAt": "2026-04-14T19:00:00Z",
  "action": "ExecuteTransactionCommand",
  "aggregateType": "Transaction",
  "aggregateId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "state": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "type": "Debit",
    "amount": 150.00,
    "description": "Pagamento fornecedor",
    "active": true,
    "createdAt": "2026-04-14T19:00:00Z"
  }
}
```

A chave no ImmuDB segue o padrão `audit:{uuid-do-AuditOutboxEvent}`, permitindo recuperação determinística por ID.

---

## Estrutura da tabela `TB_AUDIT_OUTBOX_EVENT`


| Coluna            | Tipo              | Descrição                          |
| ----------------- | ----------------- | ---------------------------------- |
| `ID`              | `uuid`            | Identificador do evento (PK)       |
| `DS_EVENT_TYPE`   | `varchar(100)`    | Tipo do evento (ex: `DomainAudit`) |
| `DS_PAYLOAD`      | `text`            | JSON completo do estado auditado   |
| `ST_PROCESSED`    | `boolean`         | Indica se foi gravado no ImmuDB    |
| `DT_PROCESSED_AT` | `timestamp`       | Instante da gravação no ImmuDB     |
| `NR_RETRY_COUNT`  | `int` (default 0) | Contador de tentativas falhas      |
| `DT_CREATED_AT`   | `timestamp`       | Criado em (herdado de `Entity`)    |


**Índice:** `IX_AUDIT_OUTBOX_EVENT_PROCESSED_CREATED` em `(ST_PROCESSED, DT_CREATED_AT)` — otimiza o polling do worker.

---

## Configuração


| Chave                                | Descrição                        | Default     |
| ------------------------------------ | -------------------------------- | ----------- |
| `ImmuDb:Host`                        | Host do ImmuDB                   | `localhost` |
| `ImmuDb:Port`                        | Porta gRPC do ImmuDB             | `3322`      |
| `ImmuDb:Username`                    | Usuário ImmuDB                   | `immudb`    |
| `ImmuDb:Password`                    | Senha ImmuDB                     | `immudb`    |
| `ImmuDb:Database`                    | Banco ImmuDB                     | `defaultdb` |
| `AuditWorker:PollingIntervalSeconds` | Intervalo entre ciclos do worker | `3`         |
| `AuditWorker:BatchSize`              | Máx. eventos por ciclo           | `50`        |
| `AuditWorker:MaxRetries`             | Tentativas antes de descartar    | `5`         |


### Docker Compose (desenvolvimento local)

No monorepo, o `**docker-compose.yml` na raiz** inclui o serviço `**immudb`** (imagem `codenotary/immudb:latest`, profile `**infra**`):

- **Portas:** `3322` (gRPC / cliente .NET) e `9497` (métricas Prometheus `/metrics`).
- **Volume:** `immudb_data` montado em `/var/lib/immudb`.
- **Healthcheck:** requisição HTTP ao endpoint de métricas em `127.0.0.1:9497/metrics` (para o `cashflow-api` poder usar `depends_on: immudb` com condição saudável).

O serviço `**cashflow-api`** recebe as variáveis `ImmuDb__Host`, `ImmuDb__Port`, `ImmuDb__Username`, `ImmuDb__Password`, `ImmuDb__Database` apontando para o host `immudb` na rede interna do compose.

O scrape do Prometheus para o ImmuDB (`job_name: immudb` → `immudb:9497`) está em `**infra/prometheus/prometheus.yml**` (compose profile `**observability**`).

---

## Garantias e trade-offs


| Garantia                   | Mecanismo                                                                                                                         |
| -------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| **Atomicidade**            | `AuditOutboxEvent` é inserido no mesmo `SaveChangesAsync` que o agregado — dentro da mesma transação PostgreSQL.                  |
| **Imutabilidade**          | ImmuDB utiliza `VerifiedSet`: cada escrita gera um hash criptográfico que pode ser verificado a qualquer momento.                 |
| **At-least-once**          | Se o worker falhar antes de marcar `Processed`, o evento é re-processado. O `VerifiedSet` é idempotente por chave `audit:{uuid}`. |
| **Resiliência**            | Após `MaxRetries` (5) falhas consecutivas, o evento é descartado do polling (não deletado — permanece auditável em PostgreSQL).   |
| **Rastreabilidade**        | `UserId` (JWT `sub`) + `OccurredAt` + `aggregateId` permitem reconstrução completa da linha do tempo por usuário ou agregado.     |
| **Disponibilidade da API** | Falha do ImmuDB não bloqueia a requisição — apenas o worker de outbox fica pendente até a recuperação.                            |


---

## Decisões

- **ImmuDB como backend imutável:** banco de dados append-only com verificação criptográfica nativa (`VerifiedSet`/`VerifiedGet`), alinhado ao requisito de não-repúdio. Alternativa descartada: tabela de auditoria em PostgreSQL (mutável — sem garantia criptográfica).
- **Outbox transacional para auditoria:** garante que nenhuma operação de negócio fique sem registro de auditoria, mesmo em caso de falha após o commit. Segue o mesmo padrão adotado para projeções MongoDB ([ADR-003](../../decisions/ADR-003-comunicacao-assincrona-rabbitmq.md)).
- `**IAuditContext` scoped:** o contexto de auditoria tem tempo de vida por requisição, garantindo isolamento entre requisições concorrentes sem concorrência de estado.
- `**CommandBase` implementa `IAuditableCommand`:** todos os records que herdam `CommandBase` expõem `UserId`/`OccurredAt` ao filtro e ao pipeline. `**IRequest` não fica na base** — caso contrário um comando como `EnqueueTransactionCommand` herdaria `IRequest` (void) e `IRequest<EnqueueResult>` via `IEnqueueCommand<>`, quebrando o `Send` do MediatR. Cada comando declara o `IRequest` adequado (`ExecuteTransactionCommand : IRequest`, enqueue apenas via `IEnqueueCommand<>`).
- `**IdentityCommandFilter` em `Security.Filters`:** a responsabilidade de extrair a identidade JWT foi movida para o projeto de segurança, onde é coesa com autenticação/autorização. O filtro itera sobre `IAuditableCommand` (não `CommandBase`), respeitando o princípio de depender de abstrações.
- **Propagação pelo boundary assíncrono via mensagem:** `UserId` e `OccurredAt` são embutidos em `EnqueueTransactionMessage` e reconstruídos pelo consumer no `ExecuteTransactionCommand`. Isso elimina a necessidade de `IHttpContextAccessor` em workers e garante que o registro de auditoria sempre reflita quem iniciou a operação, mesmo após cruzar o broker.

