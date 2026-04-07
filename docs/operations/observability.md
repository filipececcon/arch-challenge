# Observabilidade — Estratégia e Arquitetura

Este documento descreve a estratégia de observabilidade adotada para o sistema de controle de fluxo de caixa, cobrindo os três pilares: **logs**, **traces** e **métricas**.

---

## Visão Geral

O diagrama de arquitetura (Mermaid) está em [`diagrams/observability-overview.mmd`](./diagrams/observability-overview.mmd). Abra o ficheiro no IDE com extensão Mermaid ou exporte para SVG/PNG a partir do [Mermaid Live Editor](https://mermaid.live/).

---

## Pilar 1 — Logs

### Decisão arquitetural

As aplicações escrevem logs exclusivamente em `stdout` no formato JSON estruturado via **Serilog**. Um agente externo — **Fluent Bit** — lê esses logs dos containers Docker e os encaminha ao Elasticsearch.

> Diagrama de sequência do pipeline completo: [`diagrams/log-pipeline.mmd`](./diagrams/log-pipeline.mmd)

> Decisão documentada em: [ADR-011 — Fluent Bit como Ingestor de Logs](../decisions/ADR-011-fluent-bit-ingestor-de-logs.md)

### Fluent Bit: é um container “ligado” ao stdout do CashFlow?

**Não no sentido de um pipe direto** (o Fluent Bit não acopla ao processo da API nem lê o seu `stdout` como um `|` no shell).

O fluxo real é:

1. **CashFlow (e outros serviços)** escrevem em **stdout** via Serilog.
2. O **Docker Engine** captura esse stream e **persiste** em ficheiros no host (um ficheiro JSON por container, normalmente sob `/var/lib/docker/containers/<id>/<id>-json.log`). Cada linha desse ficheiro é um envelope JSON do Docker com um campo `log` que contém a linha que a aplicação imprimiu.
3. O **Fluent Bit** corre noutro **container**, com a sua própria imagem (`fluent/fluent-bit`), e obtém esses eventos de uma de duas formas típicas:
   - **INPUT `tail`** — seguir os ficheiros `*-json.log` (requer montar o diretório de logs dos containers no serviço do Fluent Bit; em **Linux** é direto; no **Docker Desktop (macOS/Windows)** o caminho do host não expõe o mesmo que em Linux — muitas equipas preferem o INPUT `docker` com *socket*).
   - **INPUT `docker`** (ou equivalente via API) — montar **`/var/run/docker.sock`** e filtrar por **nome do container**, **label** ou **ID**, para receber o mesmo stream que o Docker já agregou a partir do stdout.

Ou seja: o Fluent Bit **não** “aponta” para o container do CashFlow como um endereço de rede; ele **lê o que o Docker já registou** a partir do stdout (ficheiros ou API), processa, enriquece e envia ao Elasticsearch.

### Provisionamento no Docker Compose (esboço)

O serviço do Fluent Bit no `docker-compose` costuma ser definido assim (conceitualmente):

| Peça | Função |
|------|--------|
| **Imagem** | `fluent/fluent-bit` (tag estável, ex. `3.x`) |
| **Ficheiros de configuração** | Montados por volume, ex. `./infra/fluent-bit/fluent-bit.conf` (e `parsers.conf` se usar parser JSON do campo `log`) |
| **Volume de buffer** | Volume nomeado mapeado para o caminho usado em `storage.path` / buffer em disco (resiliência quando o ES cai) |
| **Acesso aos logs** | `tail`: bind mount de `/var/lib/docker/containers` **ou** `docker.sock` para input baseado na API Docker |
| **Rede** | Mesma rede Docker que o Elasticsearch (`OUTPUT` para `http://elasticsearch:9200` com utilizador `elastic` e respetiva palavra-passe) |
| **Dependência** | `depends_on` com Elasticsearch **healthy** |

Exemplo mínimo de `fluent-bit.conf` (ilustrativo — ajustar *inputs* e filtros ao modo escolhido, *tail* vs *docker*):

```ini
[SERVICE]
    Flush         5
    Log_Level     info
    storage.path  /var/log/flb-storage/
    storage.sync  normal

# Exemplo: saída para Elasticsearch (credenciais alinhadas ao stack local)
[OUTPUT]
    Name            es
    Match           *
    Host            elasticsearch
    Port            9200
    HTTP_User       elastic
    HTTP_Passwd     changeme
    tls             Off
    Suppress_Type_Name On
```

Os blocos **INPUT** e **FILTER** (parser `docker`, extrair JSON do campo `log`, rotear para índices `cashflow-logs-*`, etc.) dependem de se usar *tail* nos ficheiros do Docker ou o plugin **docker** com *socket*; ver [Fluent Bit — Inputs](https://docs.fluentbit.io/manual/pipeline/inputs).

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
| `container_image` | `ghcr.io/SEU_ORG/arch-challenge-cashflow-api:1.0.0` |
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

| Componente | Imagem (Compose) | Porta (host) | Responsabilidade |
|---|---|---:|---|
| Elasticsearch | `docker.elastic.co/elasticsearch/elasticsearch:8.15.3` | 9200 | Storage de logs e dados do APM |
| Kibana | `docker.elastic.co/kibana/kibana:8.15.3` | 5601 | UI (Discover, APM, dashboards) |
| Elastic APM Server | `docker.elastic.co/apm/apm-server:8.15.3` | 8200 | Ingestão de traces dos agentes |
| Fluent Bit | `fluent/fluent-bit:3.x` | — | Coleta e ingestão de logs (planejado; ver ADR-011) |
| Prometheus | `prom/prometheus:v2.53.2` | 9090 | Scrape e armazenamento de métricas |
| Grafana | `grafana/grafana:11.3.0` | 3000 | Dashboards e alertas (datasource Prometheus provisionado) |

---

## Docker Compose — observabilidade

Os serviços **Elasticsearch**, **Kibana**, **APM Server**, **Prometheus** e **Grafana** estão definidos no `docker-compose.yml` na raiz do repositório. Volumes nomeados persistem dados: `elasticsearch_data`, `prometheus_data`, `grafana_data`.

### Endereços na máquina host (desenvolvimento)

| Serviço | URL | Uso |
|---|---|---|
| Elasticsearch | http://localhost:9200 | API REST, health |
| Kibana | http://localhost:5601 | Interface web |
| Elastic APM Server | http://localhost:8200 | Endpoint dos agentes APM (ingestão) |
| Prometheus | http://localhost:9090 | UI e API de consulta |
| Grafana | http://localhost:3000 | Interface web |

### Endereços entre containers (rede `cashflow-network`)

Use estes hostnames nas variáveis de ambiente das aplicações quando rodarem **no mesmo Compose**:

| Serviço | Base URL interna |
|---|---|
| Elasticsearch | `http://elasticsearch:9200` |
| Kibana | `http://kibana:5601` |
| APM Server | `http://apm-server:8200` |
| Prometheus | `http://prometheus:9090` |
| Grafana | `http://grafana:3000` |

### Credenciais e segurança (apenas desenvolvimento local)

> **Atenção:** usuários e senhas abaixo são **somente para ambiente local**. Em produção, use segredos gerenciados, senhas fortes e TLS nas APIs.

| Serviço | Usuário | Senha | Observação |
|---|---|---|---|
| Elasticsearch (`elastic`) | `elastic` | `changeme` | Definida por `ELASTIC_PASSWORD` no Compose |
| Kibana (login na UI) | `elastic` | `changeme` | Mesmo superusuário do cluster |
| Grafana | `admin` | `admin` | `GF_SECURITY_ADMIN_*` no Compose |
| Prometheus | — | — | Sem autenticação por padrão nesta stack |
| Elastic APM Server | — | — | Sem `secret_token` neste Compose; para restringir ingestão, configure `apm-server` e os agentes com o mesmo token |

A configuração do **APM Server** em desenvolvimento está em [`infra/apm-server/apm-server.yml`](../../infra/apm-server/apm-server.yml) e usa o utilizador `elastic` com a mesma senha `changeme` para escrita no Elasticsearch.

O **Elasticsearch** está com **HTTP sem TLS** (`xpack.security.http.ssl.enabled=false`) para simplificar o Compose local; o utilizador `elastic` continua protegido por palavra-passe. Em produção, ative TLS e políticas de rede adequadas.

### Prometheus e Grafana

- Ficheiro de configuração do Prometheus: [`infra/prometheus/prometheus.yml`](../../infra/prometheus/prometheus.yml) (targets: `cashflow-api`, `dashboard-api`, `gateway` na porta interna **8080**, path `/metrics`).
- Provisionamento do datasource no Grafana: [`infra/grafana/provisioning/datasources/datasources.yml`](../../infra/grafana/provisioning/datasources/datasources.yml) (Prometheus em `http://prometheus:9090`).

Enquanto os serviços .NET não expuserem `/metrics`, os targets podem aparecer como **DOWN** no Prometheus; isso é esperado até a instrumentação estar ligada.

---

## Acessos locais (desenvolvimento) — resumo rápido

| Ferramenta | URL | Credenciais |
|---|---|---|
| Kibana | http://localhost:5601 | `elastic` / `changeme` |
| Grafana | http://localhost:3000 | `admin` / `admin` |
| Prometheus | http://localhost:9090 | — |
| Elasticsearch | http://localhost:9200 | `elastic` / `changeme` |
| Elastic APM Server | http://localhost:8200 | ingestão (sem credencial neste Compose) |
| RabbitMQ Management | http://localhost:15672 | `rabbit` / `rabbit` |
| Keycloak | http://localhost:8080 | `admin` / `admin` (consola admin) |

---

## Decisões arquiteturais relacionadas

| ADR | Decisão |
|---|---|
| [ADR-011](../decisions/ADR-011-fluent-bit-ingestor-de-logs.md) | Fluent Bit como ingestor de logs |
