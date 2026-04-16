# Observabilidade — Estratégia e Arquitetura

Este documento descreve a estratégia de observabilidade adotada para o sistema de controle de fluxo de caixa, cobrindo os três pilares: **logs**, **traces** e **métricas**.

---

## Visão Geral

O diagrama de componentes da plataforma de observabilidade está em `**[docs/architecture/diagrams/Architecture-C3 - Observability.png](../architecture/diagrams/Architecture-C3%20-%20Observability.png)`** (C3-b no modelo C4 — ver `[docs/architecture/README.md](../architecture/README.md)`).

O diagrama de fluxo de dados (Mermaid) está em `[diagrams/observability-overview.mmd](./diagrams/observability-overview.mmd)`. Abra o ficheiro no IDE com extensão Mermaid ou exporte para SVG/PNG a partir do [Mermaid Live Editor](https://mermaid.live/).

---

## Pilar 1 — Logs

### Decisão arquitetural

As aplicações escrevem logs exclusivamente em `stdout` no formato JSON estruturado via **Serilog**. Um agente externo — **Fluent Bit** — lê esses logs dos containers Docker e os encaminha ao Elasticsearch.

> Diagrama de sequência do pipeline completo: `[diagrams/log-pipeline.mmd](./diagrams/log-pipeline.mmd)`

> Decisão documentada em: [ADR-011 — Fluent Bit como Ingestor de Logs](../decisions/ADR-011-fluent-bit-ingestor-de-logs.md)

### Fluent Bit: é um container “ligado” ao stdout do CashFlow?

**Não no sentido de um pipe direto** (o Fluent Bit não acopla ao processo da API nem lê o seu `stdout` como um `|` no shell).

O fluxo real é:

1. **CashFlow (e outros serviços)** escrevem em **stdout** via Serilog.
2. O **Docker Engine** captura esse stream e **persiste** em ficheiros no host (um ficheiro JSON por container, normalmente sob `/var/lib/docker/containers/<id>/<id>-json.log`). Cada linha desse ficheiro é um envelope JSON do Docker com um campo `log` que contém a linha que a aplicação imprimiu.
3. O **Fluent Bit** corre noutro **container**, com a sua própria imagem (`fluent/fluent-bit`), e obtém esses eventos de uma de duas formas típicas:
  - **INPUT `tail`** — seguir os ficheiros `*-json.log` (requer montar o diretório de logs dos containers no serviço do Fluent Bit; em **Linux** é direto; no **Docker Desktop (macOS/Windows)** o caminho do host não expõe o mesmo que em Linux — muitas equipas preferem o INPUT `docker` com *socket*).
  - **INPUT `docker`** (ou equivalente via API) — montar `**/var/run/docker.sock**` e filtrar por **nome do container**, **label** ou **ID**, para receber o mesmo stream que o Docker já agregou a partir do stdout.

Ou seja: o Fluent Bit **não** “aponta” para o container do CashFlow como um endereço de rede; ele **lê o que o Docker já registou** a partir do stdout (ficheiros ou API), processa, enriquece e envia ao Elasticsearch.

### Provisionamento no Docker Compose (esboço)

O serviço do Fluent Bit no `docker-compose` costuma ser definido assim (conceitualmente):


| Peça                          | Função                                                                                                                               |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| **Imagem**                    | `fluent/fluent-bit` (tag estável, ex. `3.x`)                                                                                         |
| **Ficheiros de configuração** | Montados por volume, ex. `./infra/fluent-bit/fluent-bit.conf` (e `parsers.conf` se usar parser JSON do campo `log`)                  |
| **Volume de buffer**          | Volume nomeado mapeado para o caminho usado em `storage.path` / buffer em disco (resiliência quando o ES cai)                        |
| **Acesso aos logs**           | `tail`: bind mount de `/var/lib/docker/containers` **ou** `docker.sock` para input baseado na API Docker                             |
| **Rede**                      | Mesma rede Docker que o Elasticsearch (`OUTPUT` para `http://elasticsearch:9200` com utilizador `elastic` e respetiva palavra-passe) |
| **Dependência**               | `depends_on` com Elasticsearch **healthy**                                                                                           |


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

Os blocos **INPUT** e **FILTER** (parser `docker`, extrair JSON do campo `log`, rotear para índices `cashflow-logs-`*, etc.) dependem de se usar *tail* nos ficheiros do Docker ou o plugin **docker** com *socket*; ver [Fluent Bit — Inputs](https://docs.fluentbit.io/manual/pipeline/inputs).

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


| Campo                 | Valor exemplo                                       |
| --------------------- | --------------------------------------------------- |
| `container_name`      | `cashflow-api`                                      |
| `container_image`     | `ghcr.io/SEU_ORG/arch-challenge-cashflow-api:1.0.0` |
| `docker.container_id` | `abc123...`                                         |


### Índices no Elasticsearch


| Índice                      | Conteúdo              |
| --------------------------- | --------------------- |
| `cashflow-logs-YYYY.MM.DD`  | Logs da CashFlow API  |
| `dashboard-logs-YYYY.MM.DD` | Logs da Dashboard API |
| `gateway-logs-YYYY.MM.DD`   | Logs do API Gateway   |


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


| Aspecto            | Elastic Stack             | Prometheus + Grafana                           |
| ------------------ | ------------------------- | ---------------------------------------------- |
| Foco               | Logs e traces             | Métricas numéricas                             |
| Modelo de coleta   | Push (agente envia)       | Pull (Prometheus busca)                        |
| Alertas            | Limitado no tier gratuito | Alertmanager nativo e poderoso                 |
| Dashboards prontos | APM dashboards            | Comunidade com milhares de dashboards          |
| Exporters          | Poucos                    | Vasto ecossistema (RabbitMQ, PostgreSQL, etc.) |


### Métricas coletadas por serviço

**Todas as aplicações ASP.NET Core (automáticas via `prometheus-net`):**

- `http_requests_received_total` — total de requisições por rota, método e status code
- `http_request_duration_seconds` — latência de requisições (histograma)
- `http_requests_in_progress` — requisições em andamento
- `dotnet_gc_`* — métricas do Garbage Collector
- `dotnet_threadpool_*` — métricas do Thread Pool

**Infraestrutura de bancos de dados (via Prometheus Exporters — ver ADR-013):**

O padrão **Exporter** é usado para todos os bancos: um container dedicado se conecta ao banco, lê suas estatísticas internas e as expõe em `/metrics` para o Prometheus raspar.


| Banco / serviço | Exporter / origem        | Porta | Principais métricas                                                 |
| --------------- | ------------------------ | ----- | ------------------------------------------------------------------- |
| PostgreSQL      | `postgres_exporter`      | 9187  | `pg_stat_activity_count`, `pg_up`, `pg_settings_max_connections`    |
| MongoDB         | `mongodb_exporter`       | 9216  | `mongodb_ss_connections`, `mongodb_up`                              |
| Redis           | `redis_exporter`         | 9121  | `redis_memory_used_bytes`, `redis_up`                               |
| Elasticsearch   | `elasticsearch_exporter` | 9114  | `elasticsearch_jvm_memory_`*, `elasticsearch_cluster_health_status` |
| RabbitMQ        | `kbudde/rabbitmq-exporter` | 9419 | `queue_messages`, `rabbitmq_up` (nomes variam conforme o exporter)   |
| immudb          | métricas nativas (`/metrics`) | 9497 | `go_*`, `grpc_server_handled_total`, etc.                          |


### Dashboards provisionados

Os dashboards são provisionados automaticamente via `infra/grafana/provisioning/dashboards/` ao subir o Compose — sem importação manual:


| Dashboard        | Fonte                  | ID Grafana / notas |
| ---------------- | ---------------------- | ------------------ |
| PostgreSQL       | `postgres.json`        | 9628               |
| MongoDB          | `mongodb.json`         | 7353               |
| Redis            | `redis.json`           | 11835              |
| Elasticsearch    | `elasticsearch.json`   | 14191              |
| prometheus-net   | `prometheus-net.json`  | 10427 (métricas `prometheus-net` nas APIs com `/metrics`) |
| Prometheus       | `prometheus.json`      | 15489 (saúde do servidor Prometheus) |
| RabbitMQ         | `rabbitmq.json`        | 10991 (compatível com `kbudde/rabbitmq-exporter`) |
| immudb           | `immudb.json`          | Painel interno (Go/gRPC, `job=immudb`) |


### Alertas configurados

Os alertas são gerenciados pelo **Grafana Alerting** (ver ADR-014), provisionados via `infra/grafana/provisioning/alerting/`:


| Serviço       | Alerta                 | Condição                                 | Severidade |
| ------------- | ---------------------- | ---------------------------------------- | ---------- |
| cashflow-api  | Taxa de erros 5xx alta | > 5% por 2min                            | critical   |
| cashflow-api  | Latência p95 elevada   | > 1s por 2min                            | warning    |
| PostgreSQL    | Conexões altas         | > 80% do limite por 2min                 | warning    |
| PostgreSQL    | Instância fora do ar   | `pg_up < 1` por 1min                     | critical   |
| MongoDB       | Conexões altas         | > 200 por 2min                           | warning    |
| MongoDB       | Instância fora do ar   | `mongodb_up < 1` por 1min                | critical   |
| Redis         | Memória alta           | > 80% por 2min                           | warning    |
| Redis         | Instância fora do ar   | `redis_up < 1` por 1min                  | critical   |
| Elasticsearch | Heap JVM alto          | > 85% por 2min                           | critical   |
| Elasticsearch | Cluster RED            | `cluster_health_status{color="red"} > 0` | critical   |


**Política de notificação:**


| Severidade | Espera antes de notificar | Repetição      |
| ---------- | ------------------------- | -------------- |
| `critical` | 10 segundos               | A cada 1 hora  |
| `warning`  | 1 minuto                  | A cada 6 horas |


**Contact points** configurados em `infra/grafana/provisioning/alerting/contact-points.yml`:

- **Webhook** (padrão) — substituir pela URL real em produção
- **E-mail** — bloco comentado pronto para uso; requer configuração de `GF_SMTP_`* no Compose

---

## Componentes de infraestrutura


| Componente             | Imagem (Compose)                                       | Porta (host) | Responsabilidade                                          |
| ---------------------- | ------------------------------------------------------ | ------------ | --------------------------------------------------------- |
| Elasticsearch          | `docker.elastic.co/elasticsearch/elasticsearch:8.15.3` | 9200         | Storage de logs e dados do APM                            |
| Kibana                 | `docker.elastic.co/kibana/kibana:8.15.3`               | 5601         | UI (Discover, APM, dashboards)                            |
| Elastic APM Server     | `docker.elastic.co/apm/apm-server:8.15.3`              | 8200         | Ingestão de traces dos agentes                            |
| Fluent Bit             | `fluent/fluent-bit:3.x`                                | —            | Coleta e ingestão de logs (planejado; ver ADR-011)        |
| Prometheus             | `prom/prometheus:v2.53.2`                              | 9090         | Scrape e armazenamento de métricas                        |
| Grafana                | `grafana/grafana:11.3.0`                               | 3000         | Dashboards e alertas (datasource Prometheus provisionado) |
| postgres-exporter      | `prometheuscommunity/postgres-exporter:v0.15.0`        | 9187         | Exporta métricas do PostgreSQL para o Prometheus          |
| mongodb-exporter       | `percona/mongodb_exporter:0.40`                        | 9216         | Exporta métricas do MongoDB para o Prometheus             |
| redis-exporter         | `oliver006/redis_exporter:v1.62.0`                     | 9121         | Exporta métricas do Redis para o Prometheus               |
| elasticsearch-exporter | `prometheuscommunity/elasticsearch-exporter:v1.7.0`    | 9114         | Exporta métricas do Elasticsearch para o Prometheus       |
| rabbitmq-exporter      | `kbudde/rabbitmq-exporter:1.0.0-RC19`                  | 9419         | Exporta métricas do RabbitMQ para o Prometheus            |


---

## Docker Compose — observabilidade

Os serviços **Elasticsearch**, **Kibana**, **APM Server**, **Prometheus** e **Grafana** estão definidos no `docker-compose.yml` na raiz do repositório. Volumes nomeados persistem dados: `elasticsearch_data`, `prometheus_data`, `grafana_data`.

### Endereços na máquina host (desenvolvimento)


| Serviço            | URL                                            | Uso                                 |
| ------------------ | ---------------------------------------------- | ----------------------------------- |
| Elasticsearch      | [http://localhost:9200](http://localhost:9200) | API REST, health                    |
| Kibana             | [http://localhost:5601](http://localhost:5601) | Interface web                       |
| Elastic APM Server | [http://localhost:8200](http://localhost:8200) | Endpoint dos agentes APM (ingestão) |
| Prometheus         | [http://localhost:9090](http://localhost:9090) | UI e API de consulta                |
| Grafana            | [http://localhost:3000](http://localhost:3000) | Interface web                       |


### Endereços entre containers (rede `cashflow-network`)

Use estes hostnames nas variáveis de ambiente das aplicações quando rodarem **no mesmo Compose**:


| Serviço       | Base URL interna            |
| ------------- | --------------------------- |
| Elasticsearch | `http://elasticsearch:9200` |
| Kibana        | `http://kibana:5601`        |
| APM Server    | `http://apm-server:8200`    |
| Prometheus    | `http://prometheus:9090`    |
| Grafana       | `http://grafana:3000`       |


### Credenciais e segurança (apenas desenvolvimento local)

> **Atenção:** usuários e senhas abaixo são **somente para ambiente local**. Em produção, use segredos gerenciados, senhas fortes e TLS nas APIs.


| Serviço                   | Usuário   | Senha      | Observação                                                                                                        |
| ------------------------- | --------- | ---------- | ----------------------------------------------------------------------------------------------------------------- |
| Elasticsearch (`elastic`) | `elastic` | `changeme` | Definida por `ELASTIC_PASSWORD` no Compose                                                                        |
| Kibana (login na UI)      | `elastic` | `changeme` | Mesmo superusuário do cluster                                                                                     |
| Grafana                   | `admin`   | `admin`    | `GF_SECURITY_ADMIN_`* no Compose                                                                                  |
| Prometheus                | —         | —          | Sem autenticação por padrão nesta stack                                                                           |
| Elastic APM Server        | —         | —          | Sem `secret_token` neste Compose; para restringir ingestão, configure `apm-server` e os agentes com o mesmo token |


A configuração do **APM Server** em desenvolvimento está em `[infra/apm-server/apm-server.yml](../../infra/apm-server/apm-server.yml)` e usa o utilizador `elastic` com a mesma senha `changeme` para escrita no Elasticsearch.

O **Elasticsearch** está com **HTTP sem TLS** (`xpack.security.http.ssl.enabled=false`) para simplificar o Compose local; o utilizador `elastic` continua protegido por palavra-passe. Em produção, ative TLS e políticas de rede adequadas.

### Prometheus e Grafana

- Configuração do Prometheus: `[infra/prometheus/prometheus.yml](../../infra/prometheus/prometheus.yml)`
  - Targets de aplicação: `cashflow-api`, `dashboard-api`, `gateway` (porta interna **8080**, path `/metrics`)
  - Targets de infraestrutura: `postgres-exporter:9187`, `mongodb-exporter:9216`, `redis-exporter:9121`, `elasticsearch-exporter:9114`
- Provisionamento do Grafana em `infra/grafana/provisioning/`:

```
provisioning/
├── datasources/
│   └── datasources.yml       ← Prometheus como datasource padrão
├── dashboards/
│   ├── dashboards.yml        ← configuração do provider
│   ├── postgres.json         ← dashboard ID 9628
│   ├── mongodb.json          ← dashboard ID 7353
│   ├── redis.json            ← dashboard ID 11835
│   └── elasticsearch.json    ← dashboard ID 14191
└── alerting/
    ├── contact-points.yml    ← webhook (padrão) e e-mail (comentado)
    ├── notification-policies.yml ← roteamento por severidade
    └── rules.yml             ← 10 regras de alerta por serviço
```

Toda a configuração sobe automaticamente com `docker compose up` — nenhuma ação manual no Grafana é necessária.

---

## Acessos locais (desenvolvimento) — resumo rápido

| Ferramenta | URL | Usuário | Senha | Profile |
|---|---|---|---|---|
| Kibana | http://localhost:5601 | `elastic` | `changeme` | `observability` |
| Grafana | http://localhost:3000 | `admin` | `admin` | `observability` |
| Prometheus | http://localhost:9090 | — | — | `observability` |
| Elasticsearch | http://localhost:9200 | `elastic` | `changeme` | `observability` |
| Elastic APM Server | http://localhost:8200 | — | — | `observability` |
| Keycloak (admin) | http://localhost:8080 | `admin` | `admin` | `infra` |

> Credenciais de bases de dados, brokers e ferramentas de administração (pgAdmin, Mongo Express, RabbitMQ Management): **[docs/data/local-connections.md](../data/local-connections.md)**.


---

## Decisões arquiteturais relacionadas


| ADR                                                                                    | Decisão                                                     |
| -------------------------------------------------------------------------------------- | ----------------------------------------------------------- |
| [ADR-011](../decisions/ADR-011-fluent-bit-ingestor-de-logs.md)                         | Fluent Bit como ingestor de logs                            |
| [ADR-013](../decisions/ADR-013-prometheus-exporter-pattern-metricas-infraestrutura.md) | Prometheus Exporter Pattern para métricas de infraestrutura |
| [ADR-014](../decisions/ADR-014-grafana-alerting-sistema-centralizado-alertas.md)       | Grafana Alerting como sistema centralizado de alertas       |


