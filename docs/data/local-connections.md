# Conexões Locais — Bases de Dados e Brokers

Referência rápida de credenciais e URIs para o ambiente de desenvolvimento local (Docker Compose).

> **Atenção:** estas credenciais são **exclusivas para desenvolvimento local**. Em produção, use segredos gerenciados e senhas fortes.

---

## Bases de dados e brokers (conexões diretas)

Para conexão via cliente externo (DBeaver, mongosh, redis-cli, etc.):

| Serviço | Host / Porta | Usuário | Senha | Banco / Obs. |
|---|---|---|---|---|
| PostgreSQL | `localhost:5432` | `postgres` | `postgres` | Bancos `cashflow_db`, `dashboard_db` e `keycloak_db` criados pelo `init.sql` |
| MongoDB (root) | `localhost:27017` | `root` | `root` | Acesso administrativo |
| MongoDB (app) | `localhost:27017` | `cashflow` | `cashflow` | Banco `cashflow_read`, coleção `transactions` |
| Redis | `localhost:6379` | — | — | Sem autenticação neste Compose |
| RabbitMQ (AMQP) | `localhost:5672` | `rabbit` | `rabbit` | Protocolo AMQP — para a Management UI ver tabela abaixo |

### URIs prontas para uso

```
# PostgreSQL
postgresql://postgres:postgres@localhost:5432/cashflow_db
postgresql://postgres:postgres@localhost:5432/dashboard_db

# MongoDB (usuário de aplicação)
mongodb://cashflow:cashflow@localhost:27017/cashflow_read

# Redis
redis://localhost:6379
```

---

## Ferramentas de administração (interfaces web)

Acessíveis com o profile `tools` do Compose (`docker compose --profile tools up -d`):

| Ferramenta | URL | Usuário | Senha | Gerencia |
|---|---|---|---|---|
| pgAdmin | http://localhost:5050 | `admin@admin.com` | `admin` | PostgreSQL |
| Mongo Express | http://localhost:8081 | `admin` | `admin` | MongoDB |

A Management UI do RabbitMQ está disponível no profile `infra`:

| Ferramenta | URL | Usuário | Senha |
|---|---|---|---|
| RabbitMQ Management | http://localhost:15672 | `rabbit` | `rabbit` |

---

## Bancos criados automaticamente

O script [`infra/postgres/init.sql`](../../infra/postgres/init.sql) cria os seguintes bancos e usuários na primeira subida do volume PostgreSQL:

| Banco | Dono | Usado por |
|---|---|---|
| `cashflow_db` | `postgres` | CashFlow API (write model) |
| `dashboard_db` | `postgres` | Dashboard API |
| `keycloak_db` | `postgres` | Keycloak (IdP) |

O script [`infra/mongo/init.js`](../../infra/mongo/init.js) cria o usuário `cashflow` com acesso ao banco `cashflow_read` no MongoDB.

---

## Documentos relacionados

| Documento | Conteúdo |
|---|---|
| [Convenções de nomenclatura](./database-naming-conventions.md) | Padrão de nomenclatura para objetos de banco relacional |
| [ADR-006 — PostgreSQL Database per Service](../decisions/ADR-006-postgresql-database-per-service.md) | Decisão de isolamento de bancos por serviço |
| [Observabilidade — acessos locais](../operations/observability.md#acessos-locais-desenvolvimento--resumo-rápido) | URLs e credenciais das ferramentas de observabilidade |
