# ADR-013 — Prometheus Exporter Pattern para Métricas de Infraestrutura

- **Status:** Aceito
- **Data:** 2026-04-08
- **Decisores:** Time de Arquitetura

---

## Contexto

O sistema já expõe métricas de aplicação (latência HTTP, GC, thread pool) via `prometheus-net.AspNetCore` no endpoint `/metrics` de cada API. A lacuna estava nas **métricas de infraestrutura**: PostgreSQL, MongoDB, Redis e Elasticsearch não expõem endpoints Prometheus nativamente.

Era necessário decidir como coletar métricas desses bancos e integrá-las ao stack de observabilidade já existente (Prometheus + Grafana).

Três abordagens foram consideradas:

1. **Prometheus Exporter Pattern** — containers dedicados que se conectam a cada banco, leem suas estatísticas internas e as expõem em `/metrics` no formato Prometheus.
2. **Kibana Stack Monitoring** — monitorar o Elasticsearch (e apenas ele) via interface nativa do Kibana, sem Prometheus.
3. **OpenTelemetry Collector** — usar um collector centralizado com receivers específicos para cada banco.

---

## Decisão

Adotar o **Prometheus Exporter Pattern** para todos os bancos de dados, usando imagens oficiais da comunidade:

| Banco | Imagem | Porta |
|---|---|---|
| PostgreSQL | `prometheuscommunity/postgres-exporter:v0.15.0` | 9187 |
| MongoDB | `percona/mongodb_exporter:0.40` | 9216 |
| Redis | `oliver006/redis_exporter:v1.62.0` | 9121 |
| Elasticsearch | `prometheuscommunity/elasticsearch-exporter:v1.7.0` | 9114 |

Cada exporter é configurado como um container independente no `docker-compose.yml`, com `depends_on` para o banco correspondente e scrape configurado em `infra/prometheus/prometheus.yml`.

---

## Estrutura no Docker Compose

```
postgres-exporter        → scrape: postgres-exporter:9187/metrics
mongodb-exporter         → scrape: mongodb-exporter:9216/metrics
redis-exporter           → scrape: redis-exporter:9121/metrics
elasticsearch-exporter   → scrape: elasticsearch-exporter:9114/metrics
```

O Prometheus faz scrape de todos os targets a cada **15 segundos** (configuração global). O Grafana provisiona automaticamente dashboards da comunidade para cada banco via `infra/grafana/provisioning/dashboards/`.

---

## Alternativas Consideradas

### Kibana Stack Monitoring (apenas Elasticsearch)

**Prós:**
- Nativo, sem container adicional para o Elasticsearch
- Interface rica e especializada para o Elastic Stack (shards, índices, nodes, ILM)
- Sem dependência do Prometheus para monitorar o Elasticsearch

**Contras:**
- Cobre apenas o Elasticsearch — PostgreSQL, MongoDB e Redis precisariam de outra solução de qualquer forma
- Alertas no Elastic Watcher têm limitações no tier gratuito (Basic License)
- Fragmenta o monitoramento: parte no Kibana, parte no Grafana
- Equipe precisaria navegar em duas ferramentas diferentes para diagnosticar incidentes

### OpenTelemetry Collector

**Prós:**
- Padronização de telemetria (CNCF)
- Pode enviar para múltiplos backends simultaneamente (Prometheus, Elastic, etc.)
- Um único collector para todas as fontes

**Contras:**
- Receivers de banco de dados para OpenTelemetry são menos maduros que os exporters Prometheus
- Adiciona complexidade operacional (pipeline de processamento, configuração do collector)
- Ecossistema de dashboards prontos é menor que o do Prometheus

---

## Consequências

**Positivas:**
- Monitoramento **centralizado** de aplicação e infraestrutura no mesmo Grafana
- Ecossistema vasto de exporters — qualquer novo banco/serviço tem exporter disponível
- Dashboards da comunidade prontos e provisionados automaticamente (PostgreSQL ID 9628, MongoDB ID 7353, Redis ID 11835, Elasticsearch ID 14191)
- Modelo **pull** do Prometheus: os bancos não precisam saber que estão sendo monitorados
- Alertas unificados no Grafana Alerting (ver ADR-014)

**Negativas:**
- Um container extra por banco monitorado (overhead mínimo, mas real)
- As credenciais de acesso aos bancos precisam ser configuradas nos exporters — devem ser gerenciadas como segredos em produção
- Em Kubernetes, os exporters devem ser deployados como sidecars ou deployments separados

---

## Referências

- [Prometheus Exporters and Integrations](https://prometheus.io/docs/instrumenting/exporters/)
- [postgres_exporter](https://github.com/prometheus-community/postgres_exporter)
- [mongodb_exporter (Percona)](https://github.com/percona/mongodb_exporter)
- [redis_exporter](https://github.com/oliver006/redis_exporter)
- [elasticsearch_exporter](https://github.com/prometheus-community/elasticsearch_exporter)
