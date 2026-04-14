# Camada Api — ArchChallenge.CashFlow.Api

O projeto **ArchChallenge.CashFlow.Api** é o host ASP.NET Core do serviço Cashflow: expõe endpoints HTTP, compõe middlewares e extensões de infraestrutura (segurança, cache, dados, mensageria, observabilidade) e delega regras de aplicação ao **MediatR**.

---

## Responsabilidades

### Escopo da camada

- Ponto de entrada HTTP da aplicação.
- Roteamento de requisições para controllers e delegação de comandos e consultas via **MediatR** (`IMediator` / `ISender`).
- Pipeline ASP.NET Core: autenticação JWT (Bearer), autorização, métricas HTTP expostas pelo **prometheus-net** no endpoint **`/metrics`**, e health checks em **`/health/liveness`** (processo vivo, sem checagens de dependência) e **`/health/readiness`** (PostgreSQL, MongoDB, Redis e RabbitMQ).
- **Swagger/OpenAPI** com documentação interativa dos endpoints (configuração via extensões dedicadas).
- **Localização** de mensagens conforme cabeçalho **Accept-Language**, aplicada por middleware na pipeline.
- **Middleware de exceções** centralizado (`ExceptionMiddleware`), que captura falhas não tratadas e devolve resposta HTTP consistente.
- **Migração automática** do banco relacional na inicialização (`MigrateAsync`), garantindo schema atualizado antes de servir tráfego.
- Endpoint **SSE** em **`GET /api/tasks/{taskId}`** para acompanhamento assíncrono do processamento de lançamentos enfileirados (stream `text/event-stream` com polling no cache a intervalos curtos até estado final).

---

## Diagrama de Classes

### Visão estática

Visão simplificada dos controllers da borda HTTP, middlewares e contratos injetados.

```mermaid
classDiagram
    class TransactionsController {
        +Create()
        +GetById()
        +List()
    }

    class TasksControllers {
        +GetTaskStatus()
    }

    class ExceptionMiddleware
    class LocalizationMiddleware

    class IMediator {
        <<interface>>
    }

    class ITaskCacheService {
        <<interface>>
    }

    TransactionsController --> IMediator : usa
    TasksControllers --> ITaskCacheService : usa
```

**Notas:**

- `TransactionsController` aplica `[Authorize]` nas rotas de transações; comandos e consultas são enviados ao **MediatR**.
- `TasksControllers` não exige JWT no código atual: o cliente acompanha o `taskId` retornado no **202 Accepted** do POST.
- `ExceptionMiddleware` e `LocalizationMiddleware` participam da pipeline global configurada em `Program.cs` e extensões.

---

## Endpoints

### Rotas expostas

| Método | Rota | Autenticado | Descrição | Retorno |
|--------|------|-------------|-----------|---------|
| POST | `/api/transactions` | Sim | Enfileira lançamento; cabeçalho opcional `Idempotency-Key` (UUID) | `202` com `{ taskId }`; validação inválida `400` |
| GET | `/api/transactions/{id}` | Sim | Detalhe do lançamento por identificador | `200` com `TransactionResult`; não encontrado `404` |
| GET | `/api/transactions` | Sim | Lista com filtros (`type`, `active`, `minAmount`, `maxAmount`, `createdFrom`, `createdTo`) | `200` com `GetAllTransactionsResult`; parâmetros inválidos `400` |
| GET | `/api/tasks/{taskId}` | Não | Stream SSE com status da tarefa (polling interno até sucesso ou falha) | `text/event-stream`; evento inicial `not_found` se não houver tarefa |
| GET | `/metrics` | Não | Métricas no formato Prometheus | `text/plain` |
| GET | `/health/liveness` | Não | Liveness: processo respondendo, sem testar dependências | `200` JSON |
| GET | `/health/readiness` | Não | Readiness: agrega checagens marcadas como prontas (SQL, Mongo, Redis, RabbitMQ) | `200` ou `503` JSON |

---

## Diagrama de Sequência — POST `/api/transactions`

### Enfileiramento e resposta 202

Fluxo simplificado do aceite do lançamento: validação e handler genérico de enfileiramento registram a tarefa no cache e publicam mensagem no barramento de eventos.

```mermaid
sequenceDiagram
    autonumber
    participant C as Cliente
    participant EM as ExceptionMiddleware
    participant JWT as JwtBearer<br/>(UseAuthentication)
    participant TC as TransactionsController
    participant M as IMediator
    participant EH as EnqueueCommandHandler
    participant TCS as ITaskCacheService
    participant EB as IEventBus

    C->>EM: POST /api/transactions
    EM->>JWT: encaminha pipeline
    JWT->>TC: requisição autenticada
    TC->>M: Send(EnqueueTransactionCommand)
    M->>EH: Handle(command)
    EH->>TCS: registro de tarefa / idempotência
    EH->>EB: PublishAsync(mensagem)
    EB-->>EH: ok
    EH-->>M: EnqueueResult(taskId)
    M-->>TC: resultado
    TC-->>C: 202 Accepted { taskId }
```

---

## Diagrama de Sequência — SSE `GET /api/tasks/{taskId}`

### Polling a cada 500 ms

O controller mantém a conexão aberta, lê o estado no cache e envia eventos SSE; enquanto o status for pendente, aguarda **500 ms** e repete.

```mermaid
sequenceDiagram
    autonumber
    participant C as Cliente
    participant EM as ExceptionMiddleware
    participant TK as TasksControllers
    participant TCS as ITaskCacheService

    C->>EM: GET /api/tasks/{taskId} (Accept: text/event-stream)
    EM->>TK: encaminha

    loop até status final ou cancelamento
        TK->>TCS: GetAsync(taskId)
        alt tarefa inexistente
            TCS-->>TK: null
            TK-->>C: SSE data: { status: not_found, taskId }
        else tarefa encontrada
            TCS-->>TK: payload (pending, success ou failure)
            TK-->>C: SSE data: status + detalhes
            opt status == pending
                TK->>TK: Task.Delay(500 ms)
            end
        end
    end
```

Se o cliente fechar a conexão, o cancelamento do `CancellationToken` encerra o loop sem erro explícito no fluxo feliz.

---

## Decisões

### ADRs de referência

- **[ADR-004 — Backend com ASP.NET Core](../../decisions/ADR-004-backend-aspnet-core.md)** — fundamenta o uso de ASP.NET Core como stack do host, convenções de pipeline e extensibilidade via `Program.cs` e serviços.
- **[ADR-008 — Autenticação e autorização com Keycloak](../../decisions/ADR-008-autenticacao-autorizacao-keycloak.md)** — alinha a validação de JWT na API com o Identity Provider corporativo e o modelo de claims usado na autorização dos endpoints protegidos.
