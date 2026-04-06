# Observabilidade — Estratégia e Arquitetura

Este documento descreve a estratégia de observabilidade adotada para o sistema de controle de fluxo de caixa, cobrindo os três pilares: **logs**, **traces** e **métricas**.

---

## Visão Geral

```
┌──────────────────────────────────────────────────────────────────────┐
│                          APLICAÇÕES                                  │
│      [CashFlow API]   [Dashboard API]   [API Gateway (Ocelot)]      │
│           │                  │                    │                  │
│      Serilog → stdout (JSON estruturado)                            │
│      Elastic APM Agent (traces automáticos)                         │
│      prometheus-net (endpoint /metrics)                             │
└──────────────┬──────────────────────────────┬────────────────────────┘
               │ LOGS                          │ MÉTRICAS
               ▼                               ▼
    ┌─────────────────────┐         ┌──────────────────────┐
    │     Fluent Bit      │         │      Prometheus       │
    │  (ingestor de logs) │         │   (scrape /metrics)   │
    └──────────┬──────────┘         └──────────┬────────────┘
               │                               │
               ▼                               ▼
    ┌─────────────────────┐         ┌──────────────────────┐
    │    Elasticsearch    │         │       Grafana         │
    │    Elastic APM      │◄────────┤  Dashboards + Alertas │
    └──────────┬──────────┘         └──────────────────────┘
               │
               ▼
           [Kibana]
       Logs + APM Traces
```

---

## Pilar 1 — Logs

### Decisão arquitetural

As aplicações escrevem logs exclusivamente em `stdout` no formato JSON estruturado via **Serilog**. Um agente externo — **Fluent Bit** — lê esses logs dos containers Docker e os encaminha ao Elasticsearch.

> Diagrama de sequência do pipeline completo: [`diagrams/log-pipeline.mmd`](./diagrams/log-pipeline.mmd)

> Decisão documentada em: [ADR-011 — Fluent Bit como Ingestor de Logs](../decisions/ADR-011-fluent-bit-ingestor-de-logs.md)

### Por que stdout e não arquivo?

Seguindo o princípio [12-Factor App — Logs](https://12factor.net/logs), a aplicação trata logs como um stream de eventos e não se preocupa com seu destino. Isso desacopla completamente o código de produção do backend de observabilidade.

### Configuração do Serilog nas aplicações

Todas as aplicações .NET usam a seguinte configuração mínima:

```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(new ElasticsearchJsonFormatter())
    .CreateLogger();
```

O formato `ElasticsearchJsonFormatter` garante que o JSON emitido no stdout seja compatível com o schema esperado pelo Elasticsearch, incluindo campos como `@timestamp`, `level`, `message`, `fields`, etc.

### Resiliência do pipeline de logs

O Fluent Bit é configurado com **buffer em disco** persistido em volume Docker dedicado:

- Se o Elasticsearch estiver indisponível, logs acumulam em disco
- Quando o Elasticsearch se recupera, o Fluent Bit reentrega automaticamente
- As aplicações **nunca sabem** que o Elasticsearch estava fora
- Garantia de entrega: **at-least-once** (pode haver duplicatas em reconexões — aceitável para observabilidade)

### Enriquecimento automático de metadados

O Fluent Bit adiciona automaticamente aos logs os seguintes metadados Docker, sem nenhuma mudança no código das aplicações:

| Campo | Valor exemplo |
|---|---|
| `container_name` | `cashflow-api` |
| `container_image` | `cashflow/backend:latest` |
| `docker.container_id` | `abc123...` |

### Índices no Elasticsearch

| Índice | Conteúdo |
|---|---|
| `cashflow-logs-YYYY.MM.DD` | Logs da CashFlow API |
| `dashboard-logs-YYYY.MM.DD` | Logs da Dashboard API |
| `gateway-logs-YYYY.MM.DD` | Logs do API Gateway |

A retenção é gerenciada via **ILM (Index Lifecycle Management)** do Elasticsearch. Recomendação para produção: 30 dias em índice quente, delete automático após 90 dias.

---

## Pilar 2 — Traces (Distributed Tracing)

### Decisão arquitetural

Todas as aplicações .NET usam o **Elastic APM Agent** para instrumentação automática de traces distribuídos.

### O que é instrumentado automaticamente

- Requisições HTTP recebidas (ASP.NET Core)
- Chamadas HTTP de saída (HttpClient)
- Queries ao banco de dados (EF Core / Npgsql)
- Publicação e consumo de mensagens (RabbitMQ via MassTransit ou diretamente)

### Correlação logs ↔ traces

O Elastic APM Agent injeta automaticamente `trace.id` e `transaction.id` nos logs do Serilog. Isso significa que, ao ver um erro no Kibana, é possível clicar diretamente no `trace.id` e ver o trace completo da requisição que causou o erro, incluindo todos os spans entre CashFlow, Dashboard e Gateway.

### Service Map

O Kibana APM gera automaticamente um **Service Map** mostrando as dependências entre serviços com métricas de latência e taxa de erro por link, sem nenhuma configuração adicional.

### APM Server

O Elastic APM Server recebe os traces dos agentes e os indexa no Elasticsearch. Roda como container separado no `docker-compose`.

---

## Pilar 3 — Métricas

### Decisão arquitetural

As aplicações expõem métricas no padrão Prometheus via `prometheus-net.AspNetCore`. O Prometheus faz **scrape** periódico do endpoint `/metrics` de cada serviço.

### Por que Prometheus separado do Elastic Stack?

| Aspecto | Elastic Stack | Prometheus + Grafana |
|---|---|---|
| Foco | Logs e traces | Métricas numéricas |
| Modelo de coleta | Push (agente envia) | Pull (Prometheus busca) |
| Alertas | Limitado no tier gratuito | Alertmanager nativo e poderoso |
| Dashboards prontos | APM dashboards | Comunidade com milhares de dashboards |
| Exporters | Poucos | Vasto ecossistema (RabbitMQ, PostgreSQL, etc.) |

### Métricas coletadas por serviço

**Todas as aplicações ASP.NET Core (automáticas via `prometheus-net`):**
- `http_requests_total` — total de requisições por rota, método e status code
- `http_request_duration_seconds` — latência de requisições (histograma)
- `dotnet_gc_*` — métricas do Garbage Collector
- `dotnet_threadpool_*` — métricas do Thread Pool

**RabbitMQ (via `rabbitmq_prometheus` plugin nativo):**
- `rabbitmq_queue_messages` — mensagens pendentes por fila
- `rabbitmq_queue_messages_published_total` — taxa de publicação
- `rabbitmq_queue_messages_delivered_total` — taxa de consumo

**PostgreSQL (via `postgres_exporter`):**
- `pg_stat_activity_count` — conexões ativas
- `pg_stat_user_tables_*` — acessos às tabelas

### Alertas configurados

| Alerta | Condição | Severidade |
|---|---|---|
| Alta latência CashFlow | `http_request_duration_seconds{job="cashflow-api"} > 2s` por 5min | Warning |
| Alta taxa de erro | Taxa de 5xx > 1% por 5min | Critical |
| Fila RabbitMQ crescendo | `rabbitmq_queue_messages > 1000` por 10min | Warning |
| Dashboard sem consumir | Sem `messages_delivered_total` por 5min | Critical |
| PostgreSQL sem conexão | `pg_up == 0` | Critical |

Os alertas são roteados via **Alertmanager** para os canais configurados (Slack, e-mail, PagerDuty).

---

## Componentes de infraestrutura

| Componente | Imagem Docker | Porta | Responsabilidade |
|---|---|---|---|
| Elasticsearch | `elasticsearch:8.x` | 9200 | Storage de logs e traces |
| Kibana | `kibana:8.x` | 5601 | Visualização de logs e APM |
| Elastic APM Server | `elastic/apm-server:8.x` | 8200 | Recepção de traces |
| Fluent Bit | `fluent/fluent-bit:3.x` | — | Coleta e ingestão de logs |
| Prometheus | `prom/prometheus:latest` | 9090 | Coleta e storage de métricas |
| Grafana | `grafana/grafana:latest` | 3000 | Dashboards e alertas |

---

## Acessos locais (desenvolvimento)

| Ferramenta | URL | Credenciais |
|---|---|---|
| Kibana | http://localhost:5601 | elastic / changeme |
| Grafana | http://localhost:3000 | admin / admin |
| Prometheus | http://localhost:9090 | — |
| RabbitMQ Management | http://localhost:15672 | rabbit / rabbit |
| Elasticsearch | http://localhost:9200 | elastic / changeme |

---

## Decisões arquiteturais relacionadas

| ADR | Decisão |
|---|---|
| [ADR-011](../decisions/ADR-011-fluent-bit-ingestor-de-logs.md) | Fluent Bit como ingestor de logs |
