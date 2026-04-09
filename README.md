# Desafio Técnico — Arquiteto de Soluções

Sistema de controle de fluxo de caixa para comerciantes, com registro de lançamentos (débitos e créditos) e consolidado diário.

## Manual de deploy

**[→ Manual passo a passo: ambiente local e produção (Kubernetes)](./docs/operations/manual-deploy.md)** — único ponto de entrada para subir o projeto no desenvolvimento (Docker Compose ou serviços na máquina) e no ambiente produtivo.

---

## Visão Geral

O sistema é composto por dois serviços independentes:

| Serviço | Responsabilidade |
|---|---|
| **CashFlow** | Registro e gestão de lançamentos financeiros (débitos e créditos) |
| **Dashboard** | Consolidado diário e relatórios de saldo |

Os serviços se comunicam de forma **assíncrona via RabbitMQ**, garantindo que o CashFlow continue operando mesmo que o Dashboard esteja indisponível.

---

## Stack Tecnológica

| Camada | Tecnologia |
|---|---|
| API Gateway | Ocelot (ASP.NET Core) |
| Backend | ASP.NET Core (.NET 8) |
| Frontend | Angular 19 + Tailwind CSS |
| Banco de dados relacional | PostgreSQL |
| Banco de dados de leitura | MongoDB (read model / projeções) |
| Cache | Redis |
| Mensageria | RabbitMQ |
| Autenticação | Keycloak (OAuth 2.0 / OIDC) |
| Observabilidade | Elastic Stack (APM, Kibana), Prometheus, Grafana |
| Containers | Docker / Docker Compose |

---

## Estrutura do Repositório

```
arch-challenge/
├── services/
│   ├── gateway/                  ← API Gateway (Ocelot) — ponto único de entrada
│   ├── cashflow/                 ← API ASP.NET Core (CashFlow)
│   ├── dashboard/                ← API ASP.NET Core (Dashboard)
│   └── frontend/                 ← SPA Angular unificada (ver ADR-010)
│       └── src/app/
│           ├── core/             ← Auth, guards, interceptors
│           └── features/         ← Módulos por domínio (ex.: cashflow, dashboard)
├── infra/                        ← Plataforma: K8s (Kustomize), Postgres, Keycloak
│   ├── k8s/                      ← Manifests e overlay de produção
│   ├── postgres/                 ← init.sql (Compose / referência DB)
│   └── keycloak/                 ← Realm export (dev)
├── docs/
│   ├── architecture/             ← Diagramas C4 e visão arquitetural
│   ├── security/                 ← Documentação de segurança
│   ├── decisions/                ← ADRs (Architecture Decision Records)
│   └── operations/               ← Estratégia de operação e observabilidade
├── docker-compose.yml
└── README.md
```

---

## Como Executar Localmente

### Pré-requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado e em execução
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/) e [Angular CLI](https://angular.dev/tools/cli)

### 1. Subir a infraestrutura

O `docker-compose.yml` é organizado em **profiles**. Suba apenas o que precisa:

| Profile | O que sobe |
|---------|------------|
| `infra` | PostgreSQL, MongoDB, RabbitMQ, Redis, Keycloak |
| `observability` | Elasticsearch, Kibana, APM Server, Prometheus, Grafana + exporters |
| `tools` | pgAdmin (PostgreSQL UI), Mongo Express (MongoDB UI) |
| `apps` | cashflow-api, dashboard-api, gateway, frontend (em contêiner) |

```bash
# Só infraestrutura — para desenvolver as APIs fora do Docker
docker compose --profile infra up -d

# Infraestrutura + stack de observabilidade
docker compose --profile infra --profile observability up -d

# Stack completa (tudo em contêiner, com build das imagens .NET)
docker compose --profile infra --profile observability --profile tools --profile apps up -d --build
```

> **Nota:** Elasticsearch e Kibana podem levar 1–3 minutos até ficarem *healthy* na primeira subida. Recomenda-se ao menos **4 GB de RAM** atribuídos ao Docker Desktop.

### 2. Keycloak (realm `cashflow`)

Na primeira subida do container, o Keycloak importa o realm definido em [`infra/keycloak/cashflow-realm.json`](./infra/keycloak/cashflow-realm.json) (alinhado a [docs/security/authorization.md](./docs/security/authorization.md) e [ADR-008](./docs/decisions/ADR-008-autenticacao-autorizacao-keycloak.md)):

- **Admin master:** http://localhost:8080 — `admin` / `admin` (apenas desenvolvimento)
- **Realm:** `cashflow` — client `cashflow-frontend` (público, PKCE), `cashflow-api`, `dashboard-api` (confidenciais; secrets de dev no JSON)
- **Mappers de audience** nos frontends para incluir `cashflow-api` e `dashboard-api` no JWT (como em [docs/security/authentication.md](./docs/security/authentication.md))

Usuários de teste (senha **`password`** em todos):

| Usuário | Roles |
|---------|--------|
| `joao.comerciante` | `comerciante` |
| `maria.gestor` | `gestor` |
| `admin.cashflow` | `admin` (compõe `comerciante` + `gestor`) |

Se você já tinha um banco Keycloak antigo no volume do Postgres, o import pode não reaplicar o arquivo. Para forçar um realm novo: remova o volume do Postgres (`docker compose down -v`) e suba de novo (apaga também os dados das APIs).

### 3. Executar o API Gateway

```bash
cd services/gateway
dotnet restore
dotnet run
```

Gateway disponível em: http://localhost:5000
Swagger unificado em: http://localhost:5000/swagger

### 4. Executar o backend do CashFlow

```bash
cd services/cashflow
dotnet restore
dotnet run
```

API disponível em: http://localhost:5001

### 5. Executar o backend do Dashboard

```bash
cd services/dashboard
dotnet restore
dotnet run
```

API disponível em: http://localhost:5002

### 6. Executar o Frontend

**Opção A — fora do Docker (desenvolvimento com hot reload):**

```bash
cd services/frontend
npm install
ng serve
```

**Opção B — em contêiner** (via profile `apps`, junto com as APIs):

```bash
docker compose --profile infra --profile apps up -d --build
```

Disponível em: http://localhost:4200

- Rota `/cashflow` — módulo de lançamentos (débitos e créditos)
- Rota `/dashboard` — módulo de consolidado diário

---

## Documentação

| Documento | Descrição |
|---|---|
| [Arquitetura](./docs/architecture/README.md) | Diagramas C4 — Context, Container, Component e Code |
| [Segurança](./docs/security/README.md) | Estratégia de segurança completa — autenticação, RBAC, proteção de APIs e dados |
| [Decisões (ADRs)](./docs/decisions/) | Registro de todas as decisões arquiteturais |
| [Operação — observabilidade](./docs/operations/observability.md) | Logs, traces e métricas |
| [Operação — manual de deploy](./docs/operations/manual-deploy.md) | Passo a passo: ambiente local e produção (Kubernetes) |
| [Operação — Kubernetes](./docs/operations/kubernetes.md) | Deploy em cluster (Kustomize), dependências e pós-deploy |
| [Infraestrutura](./infra/README.md) | Pasta `infra/`: estrutura, capacidades (K8s, Postgres, Keycloak) e ligação ao Compose |

### ADRs

| # | Decisão | Status |
|---|---|---|
| [ADR-001](./docs/decisions/ADR-001-monorepo-com-cicd-independente.md) | Monorepo com CI/CD independente por serviço | Aceito |
| [ADR-002](./docs/decisions/ADR-002-separacao-cashflow-dashboard.md) | Separação em dois bounded contexts | Aceito |
| [ADR-003](./docs/decisions/ADR-003-comunicacao-assincrona-rabbitmq.md) | Comunicação assíncrona via RabbitMQ | Aceito |
| [ADR-004](./docs/decisions/ADR-004-backend-aspnet-core.md) | Backend com ASP.NET Core | Aceito |
| [ADR-005](./docs/decisions/ADR-005-frontend-angular.md) | Frontend com Angular | Aceito |
| [ADR-006](./docs/decisions/ADR-006-postgresql-database-per-service.md) | PostgreSQL com Database per Service | Aceito |
| [ADR-007](./docs/decisions/ADR-007-formato-mensagens-json.md) | Formato de mensagens JSON sem schema registry | Aceito |
| [ADR-008](./docs/decisions/ADR-008-autenticacao-autorizacao-keycloak.md) | Autenticação e autorização com Keycloak | Aceito |
| [ADR-009](./docs/decisions/ADR-009-api-gateway-ocelot.md) | API Gateway com Ocelot | Aceito |
| [ADR-010](./docs/decisions/ADR-010-frontend-unificado-com-feature-modules.md) | Frontend unificado com feature modules lazy-loaded | Aceito |
| [ADR-011](./docs/decisions/ADR-011-fluent-bit-ingestor-de-logs.md) | Fluent Bit como ingestor de logs | Aceito |
| [ADR-012](./docs/decisions/ADR-012-specification-pattern-read-repository.md) | Specification Pattern no ReadRepository | Aceito |
| [ADR-013](./docs/decisions/ADR-013-prometheus-exporter-pattern-metricas-infraestrutura.md) | Prometheus Exporter Pattern para métricas de infraestrutura | Aceito |
| [ADR-014](./docs/decisions/ADR-014-grafana-alerting-sistema-centralizado-alertas.md) | Grafana Alerting como sistema centralizado de alertas | Aceito |

---

## Requisitos Não Funcionais Atendidos

| Requisito | Como é atendido |
|---|---|
| CashFlow não pode cair se Dashboard falhar | Comunicação assíncrona via RabbitMQ — sem dependência direta entre os serviços |
| Dashboard suporta 50 req/s com no máximo 5% de perda | RabbitMQ como buffer de carga + escalabilidade horizontal do Dashboard |

---

## Testes

```bash
# Testes unitários — CashFlow
cd services/cashflow
dotnet test

# Testes unitários — Dashboard
cd services/dashboard
dotnet test
```
