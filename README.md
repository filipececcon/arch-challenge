# Desafio Técnico — Arquiteto de Soluções

Sistema de controle de fluxo de caixa para comerciantes, com registro de lançamentos (débitos e créditos) e consolidado diário.

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
| Frontend | Angular 17+ |
| Banco de dados | PostgreSQL |
| Mensageria | RabbitMQ |
| Autenticação | Keycloak (OAuth 2.0 / OIDC) |
| Containers | Docker / Docker Compose |

---

## Estrutura do Repositório

```
arch-challenge/
├── services/
│   ├── gateway/                  ← API Gateway (Ocelot) — ponto único de entrada
│   ├── cashflow/
│   │   └── backend/              ← API ASP.NET Core
│   ├── dashboard/
│   │   └── backend/              ← API ASP.NET Core
│   └── frontend/                 ← SPA Angular unificada (ver ADR-010)
│       └── src/app/
│           ├── core/             ← Auth, guards, interceptors
│           ├── shared/           ← Componentes reutilizáveis
│           ├── cashflow/         ← Feature module lazy-loaded (lançamentos)
│           └── dashboard/        ← Feature module lazy-loaded (consolidado)
├── shared/
│   └── contracts/                ← Contratos de eventos entre serviços
├── infra/                        ← IaC e configurações de infraestrutura
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

```bash
docker-compose up -d
```

Isso irá subir:
- PostgreSQL (porta 5432)
- RabbitMQ (porta 5672, Management UI em http://localhost:15672)
- Keycloak (porta 8080 — http://localhost:8080)

### 2. Configurar o Keycloak

Acesse http://localhost:8080 com as credenciais padrão (`admin` / `admin`) e:

1. Crie o realm `cashflow`
2. Crie os clients `cashflow-frontend` e `dashboard-frontend` (public, PKCE)
3. Crie os roles `comerciante`, `gestor` e `admin`
4. Crie um usuário de teste e atribua um role

> Em breve: script de configuração automática via Keycloak Import.

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
cd services/cashflow/backend
dotnet restore
dotnet run
```

API disponível em: http://localhost:5001

### 5. Executar o backend do Dashboard

```bash
cd services/dashboard/backend
dotnet restore
dotnet run
```

API disponível em: http://localhost:5002

### 6. Executar o Frontend

```bash
cd services/frontend
npm install
ng serve
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
| [Operação](./docs/operations/observability.md) | Estratégia de observabilidade — logs, traces e métricas |

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
cd services/cashflow/backend
dotnet test

# Testes unitários — Dashboard
cd services/dashboard/backend
dotnet test
```
