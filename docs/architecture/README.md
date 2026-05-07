# Arquitetura do Sistema — Diagramas C4

O sistema é documentado seguindo o [modelo C4](https://c4model.com/), que descreve a arquitetura em quatro níveis progressivos de detalhamento.

> **Fonte:** Os diagramas foram gerados com [draw.io](https://www.drawio.com/) — o arquivo fonte editável está em [`diagrams/Architecture.drawio`](./diagrams/Architecture.drawio).

---

## Nível 1 — Context (C1)

Visão de mais alto nível: mostra o sistema como uma caixa preta e seus relacionamentos com usuários e sistemas externos.

![Diagrama C1 — Context](./diagrams/Architecture-C1%20-%20Context.png)

**O que este diagrama mostra:**
- Os atores que interagem com o sistema (Comerciante e Gestor)
- O sistema de Controle de Fluxo de Caixa como unidade
- Dependências externas: Keycloak (autenticação)

---

## Nível 2 — Container (C2)

Detalha os containers que compõem o sistema: aplicações, bancos de dados, mensageria e serviços de suporte.

![Diagrama C2 — Container](./diagrams/Architecture-C2%20-%20Container.png)

**O que este diagrama mostra:**
- **API Gateway (Ocelot)** — ponto único de entrada, autenticação JWT e roteamento
- **CashFlow Backend** — API ASP.NET Core para registro de lançamentos
- **Dashboard Backend** — API ASP.NET Core para consolidado diário
- **Frontend SPA Unificada (Angular 19 + Tailwind CSS)** — aplicação única com feature modules lazy-loaded para CashFlow e Dashboard (ver ADR-010)
- **PostgreSQL** — banco de dados dedicado por serviço (Database per Service)
- **MongoDB** — read model / projeções da CashFlow API
- **Redis** — cache (ex.: task IDs para operações SSE)
- **RabbitMQ** — broker de mensagens para comunicação assíncrona entre serviços
- **Keycloak** — Identity Provider (OAuth 2.0 / OIDC)

---

## Nível 3 — Component (C3)

O nível C3 está dividido em **dois diagramas complementares**, separando as responsabilidades de negócio da plataforma de observabilidade.

### C3-a — Componentes de Negócio

Aprofunda a visão interna dos serviços de negócio (CashFlow, Dashboard e Gateway), mostrando os componentes e suas responsabilidades.

![Diagrama C3 — Components](./diagrams/Architecture-C3%20-%20Components.png)

**O que este diagrama mostra:**
- Camadas internas das APIs (Controllers, Services, Repositories, Domain)
- Publicação e consumo de eventos via RabbitMQ (`TransactionRegisteredIntegrationEvent`, nome de negócio `TransactionExecuted` no outbox — exchange `cashflow.events`, fila `dashboard.transaction.processed`)
- Integração do Gateway com o Keycloak para validação de JWT
- Separação clara entre os bounded contexts CashFlow e Dashboard

---

### C3-b — Observabilidade

Detalha os componentes que compõem a plataforma de observabilidade, organizada em quatro tiers independentes com exporters de métricas de infraestrutura.

![Diagrama C3 — Observability](./diagrams/Architecture-C3%20-%20Observability.png)

**O que este diagrama mostra:**

| Tier | Componentes | Responsabilidade |
|---|---|---|
| **Logs** | FluentBit, Elasticsearch, Kibana | Coleta (stdout dos containers), armazenamento e visualização de logs estruturados |
| **Tracing** | Elastic APM | Coleta e armazenamento de traces distribuídos (spans entre Gateway, CashFlow e Dashboard) |
| **Metrics** | Prometheus | Scraping periódico do endpoint `/metrics` dos serviços de negócio |
| **Monitoring & Alerts** | Grafana | Dashboards consolidados e alertas baseados nas métricas do Prometheus |
| **Metrics Exporters** | Postgres Exporter, Mongo Exporter, Redis Exporter, Elastic Exporter | Agentes que expõem métricas internas de cada banco/infra no padrão Prometheus (`/metrics`) |

**Fluxos de dados no diagrama:**

- `STD` — aplicações emitem logs estruturados para `stdout`; FluentBit coleta e encaminha ao Elasticsearch
- `LDB` — conexão de logs com o Elasticsearch (FluentBit, Elastic APM e Elastic Exporter)
- `SCPR` — Prometheus raspa os endpoints `/metrics` dos exporters e das APIs
- `PG / MG / RD` — conexões de leitura dos exporters com PostgreSQL, MongoDB e Redis respectivamente

> **Nota:** O FluentBit está planejado conforme [ADR-011](../decisions/ADR-011-fluent-bit-ingestor-de-logs.md) mas ainda não está provisionado no `docker-compose.yml`. Os demais componentes já estão ativos. Ver [`observability.md`](../operations/observability.md) para detalhes de configuração e acessos locais.

---

## Nível 4 — Code (C4)

Diagrama de classes gerado diretamente pela IDE, detalhando a estrutura de código de cada componente.

A documentação detalhada de cada camada do serviço **Cashflow API** está disponível em:

**[cashflow/ — Arquitetura por Camadas](./cashflow/README.md)**

| Camada | Arquivo |
|--------|---------|
| **Dados (capacidade)** — PostgreSQL, MongoDB, ImmuDB | [data/README.md](../data/README.md) |
| Api | [layer-01-api.md](./cashflow/layer-01-api.md) |
| Application | [layer-02-application.md](./cashflow/layer-02-application.md) |
| Domain + Shared | [layer-03-domain.md](./cashflow/layer-03-domain.md) |
| Infrastructure.Data.Relational | [layer-04-relational.md](./cashflow/layer-04-relational.md) |
| Infrastructure.Data.Documents | [layer-05-documents.md](./cashflow/layer-05-documents.md) |
| Infrastructure.CrossCutting.Messaging | [layer-06-messaging.md](./cashflow/layer-06-messaging.md) |
| Infrastructure.CrossCutting.Caching | [layer-07-caching.md](./cashflow/layer-07-caching.md) |
| Infrastructure.CrossCutting.Security | [layer-08-security.md](./cashflow/layer-08-security.md) |
| Imutável — auditoria (Application + Immutable + Agents.Outbox) | [layer-09-immutable.md](./cashflow/layer-09-immutable.md) |
| I18n (mensagens e cultura) | [layer-10-i18n.md](./cashflow/layer-10-i18n.md) |

---

## Decisões arquiteturais relacionadas

### Negócio e infraestrutura

| ADR | Decisão | Diagrama |
|---|---|---|
| [ADR-002](../decisions/ADR-002-separacao-cashflow-dashboard.md) | Separação em dois bounded contexts: CashFlow e Dashboard | C3-a |
| [ADR-003](../decisions/ADR-003-comunicacao-assincrona-rabbitmq.md) | Comunicação assíncrona via RabbitMQ | C3-a |
| [ADR-004](../decisions/ADR-004-backend-aspnet-core.md) | Backend com ASP.NET Core | C3-a |
| [ADR-005](../decisions/ADR-005-frontend-angular.md) | Frontend com Angular 19 + Tailwind CSS | C2 |
| [ADR-008](../decisions/ADR-008-autenticacao-autorizacao-keycloak.md) | Autenticação e autorização com Keycloak | C2, C3-a |
| [ADR-009](../decisions/ADR-009-api-gateway-ocelot.md) | API Gateway com Ocelot | C2, C3-a |
| [ADR-010](../decisions/ADR-010-frontend-unificado-com-feature-modules.md) | Frontend unificado com feature modules lazy-loaded | C2 |
| [ADR-016](../decisions/ADR-016-immudb-armazenamento-imutavel-auditoria.md) | ImmuDB como armazenamento imutável para auditoria (CashFlow) | — |

### Observabilidade

| ADR | Decisão | Diagrama |
|---|---|---|
| [ADR-011](../decisions/ADR-011-fluent-bit-ingestor-de-logs.md) | Fluent Bit como ingestor de logs | C3-b (Logs Tier) |
| [ADR-013](../decisions/ADR-013-prometheus-exporter-pattern-metricas-infraestrutura.md) | Prometheus Exporter Pattern para métricas de infraestrutura | C3-b (Metrics Exporters) |
| [ADR-014](../decisions/ADR-014-grafana-alerting-sistema-centralizado-alertas.md) | Grafana Alerting como sistema centralizado de alertas | C3-b (Monitoring & Alerts) |
