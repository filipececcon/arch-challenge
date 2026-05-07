# ADR-014 — Grafana Alerting como Sistema Centralizado de Alertas

- **Status:** Aceito
- **Data:** 2026-04-08
- **Decisores:** Time de Arquitetura

---

## Contexto

Com o Prometheus coletando métricas das APIs e dos bancos de dados (ver ADR-013), era necessário definir **onde e como configurar alertas** para situações críticas: serviço fora do ar, latência elevada, uso excessivo de recursos, etc.

O stack de observabilidade já possui dois sistemas com capacidade de alertas:

1. **Prometheus Alertmanager** — componente oficial do ecossistema Prometheus para roteamento e envio de alertas.
2. **Grafana Alerting (Unified Alerting)** — sistema de alertas nativo do Grafana, reescrito na versão 9+ com suporte a múltiplos datasources.
3. **Elastic Watcher** — sistema de alertas do Elasticsearch, disponível com restrições no tier Basic.

---

## Decisão

Adotar o **Grafana Alerting (Unified Alerting)** como sistema centralizado de alertas, eliminando a necessidade do Alertmanager como container separado.

As regras de alerta são **provisionadas via arquivos YAML** em `infra/grafana/provisioning/alerting/`, garantindo que toda a configuração esteja em código e no controle de versão:

```
infra/grafana/provisioning/alerting/
├── contact-points.yml ← para onde vai o alerta (webhook, e-mail, Slack, etc.)
├── notification-policies.yml ← roteamento por severidade e agrupamento
└── rules.yml ← regras de alerta por serviço
```

### Regras configuradas

| Serviço | Alerta | Severidade |
|---|---|---|
| cashflow-api | Taxa de erros 5xx > 5% | critical |
| cashflow-api | Latência p95 > 1s | warning |
| PostgreSQL | Conexões > 80% do limite | warning |
| PostgreSQL | Instância fora do ar | critical |
| MongoDB | Conexões > 200 | warning |
| MongoDB | Instância fora do ar | critical |
| Redis | Memória > 80% | warning |
| Redis | Instância fora do ar | critical |
| Elasticsearch | Heap JVM > 85% | critical |
| Elasticsearch | Cluster status RED | critical |

### Política de notificação

| Severidade | Espera antes de notificar | Repetição |
|---|---|---|
| `critical` | 10 segundos | A cada 1 hora |
| `warning` | 1 minuto | A cada 6 horas |

---

## Alternativas Consideradas

### Prometheus Alertmanager

**Prós:**
- Componente oficial e amplamente adotado no ecossistema Prometheus
- Suporte nativo a inibição (silenciar alertas derivados quando a causa-raiz já está alertando)
- Integração direta com as regras de alerta do Prometheus (arquivos `.rules`)

**Contras:**
- Requer um **container adicional** (`prom/alertmanager`) com sua própria configuração
- As regras ficam distribuídas entre Prometheus (onde são definidas) e Alertmanager (onde são roteadas) — duas ferramentas para gerenciar
- Não tem UI integrada para visualizar o estado dos alertas ao lado dos dashboards
- Para ver dashboards **e** alertas, o operador precisa navegar entre Grafana e Alertmanager
- A comunidade e os próprios mantenedores do Grafana recomendam migrar para o Grafana Alerting em novos projetos

### Elastic Watcher

**Prós:**
- Nativo ao Elasticsearch, sem container adicional para alertas do Elastic Stack

**Contras:**
- Disponível com funcionalidades limitadas no tier Basic (gratuito)
- Não cobre métricas do Prometheus — só dados no Elasticsearch
- Sintaxe de configuração complexa (JSON/Painless scripts)
- Fragmentaria ainda mais o sistema de alertas: parte no Elastic, parte em outro lugar

---

## Consequências

**Positivas:**
- **Único painel** para dashboards e alertas — o operador vê o gráfico disparando e o alerta ao mesmo tempo
- Toda a configuração de alertas está em **código versionado** (provisioning YAML) — sem configuração manual pela UI
- Ao subir `docker compose up`, os alertas já estão ativos automaticamente
- Suporte nativo a múltiplos **contact points**: webhook, e-mail, Slack, PagerDuty, Teams, OpsGenie — configuráveis no mesmo arquivo
- **Sem container adicional** do Alertmanager (menos overhead no ambiente de desenvolvimento)
- Grafana Alerting usa as mesmas queries PromQL dos painéis — sem duplicação de lógica

**Negativas:**
- Grafana Alerting não suporta **inibição** tão granular quanto o Alertmanager (ex: não disparar alerta de latência se o serviço já está down)
- As regras de alerta ficam acopladas ao Grafana — se o backend de métricas mudar (ex: migrar para Thanos), as regras precisam ser avaliadas novamente
- Em produção, o Grafana precisa de persistência confiável (banco de dados externo) para não perder o estado dos alertas em restarts

### Configuração para produção

Em produção, recomenda-se:
- Substituir o **webhook placeholder** por integrações reais (Slack, PagerDuty, e-mail corporativo)
- Configurar `GF_SMTP_*` nas variáveis de ambiente do Grafana para envio de e-mail
- Usar banco de dados externo para o Grafana (`GF_DATABASE_*`) em vez do SQLite padrão

---

## Referências

- [Grafana Alerting — documentação oficial](https://grafana.com/docs/grafana/latest/alerting/)
- [Grafana Alerting vs Prometheus Alertmanager](https://grafana.com/blog/2021/06/14/the-new-unified-alerting-system-for-grafana/)
- [Provisioning Grafana Alerting via files](https://grafana.com/docs/grafana/latest/alerting/set-up/provision-alerting-resources/file-provisioning/)
