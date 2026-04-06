# ADR-011 — Fluent Bit como Ingestor de Logs

- **Status:** Aceito
- **Data:** 2026-04-05
- **Decisores:** Time de Arquitetura

---

## Contexto

O sistema é composto por três serviços que precisam gerar logs estruturados para fins de observabilidade, debugging e auditoria operacional:

- **CashFlow API** — registra lançamentos financeiros
- **Dashboard API** — consolida saldo diário
- **API Gateway (Ocelot)** — ponto único de entrada, roteia requisições

A stack de observabilidade adotada utiliza **Elasticsearch** como backend de armazenamento e indexação de logs, e **Kibana** como ferramenta de visualização.

A questão arquitetural central é: **como os logs das aplicações chegam ao Elasticsearch?**

Existem duas abordagens principais:

1. **Serilog com sink direto para Elasticsearch** — a aplicação escreve diretamente no Elasticsearch via biblioteca cliente.
2. **Aplicação escreve em stdout + agente de coleta** — um processo externo (Fluent Bit, Filebeat, etc.) lê os logs e encaminha ao Elasticsearch.

---

## Decisão

Adotar **Fluent Bit** como agente de coleta e ingestão de logs, com as aplicações escrevendo exclusivamente em `stdout` no formato JSON estruturado via Serilog.

O Fluent Bit roda como um container separado no `docker-compose`, monta o diretório de logs do Docker via volume e encaminha os logs ao Elasticsearch com buffer em disco para garantir resiliência.

### Fluxo adotado

```
[CashFlow API]  [Dashboard API]  [API Gateway]
      │                │                │
      └────────────────┴────────────────┘
              Serilog → stdout (JSON)
                        │
        /var/lib/docker/containers/*/*.log
                        │
                   [Fluent Bit]          ← container separado
                        │
               buffer em disco           ← resiliência
                        │
               [Elasticsearch]
                        │
                    [Kibana]
```

### Configuração de buffer em disco (resiliência)

O Fluent Bit é configurado com `storage.type filesystem`, persistindo chunks não entregues em um volume Docker dedicado. Em caso de indisponibilidade do Elasticsearch, os logs acumulam em disco e são reentregues automaticamente quando o serviço se recupera, sem qualquer impacto ou conhecimento por parte das aplicações.

---

## Alternativas Consideradas

### Serilog com sink direto para Elasticsearch (`Serilog.Sinks.Elasticsearch`)

**Prós:**
- Zero componentes adicionais na infraestrutura
- Logs aparecem no Kibana com baixíssima latência
- Correlação com Elastic APM funciona nativamente

**Contras:**
- **Acoplamento direto entre a aplicação e o Elasticsearch** — se o Elasticsearch estiver lento ou indisponível, as tentativas de escrita do sink podem impactar threads e latência da aplicação
- **Sem buffer persistente nativo** — uma queda do Elasticsearch pode causar perda de logs não entregues, a menos que seja configurado manualmente um buffer em arquivo (o que recria parcialmente o que um agente já oferece nativamente)
- **Não é a abordagem recomendada pelo Elastic** — o próprio Elastic recomenda a cadeia `App → agente de coleta → Elasticsearch` para ambientes de produção
- **Enriquecimento de metadados manual** — metadados de infraestrutura (nome do container, imagem Docker, labels) precisam ser adicionados via Serilog Enrichers; um agente adiciona isso automaticamente

**Descartado** em favor de uma abordagem mais resiliente e desacoplada.

### Filebeat (Elastic)

**Prós:**
- Integração nativa e otimizada com toda a stack Elastic (Elasticsearch, Kibana, Elastic APM)
- Autodiscovery de containers Docker com enriquecimento automático de metadados
- Suporte a módulos pré-configurados para tecnologias comuns

**Contras:**
- **Mais pesado** — consome significativamente mais memória que o Fluent Bit (~60MB vs ~1MB em repouso)
- **Menos flexível para múltiplos destinos** — projetado primariamente para a stack Elastic; adicionar outros destinos (ex: Grafana Loki, S3) é mais trabalhoso
- **Não é o padrão cloud-native** — Kubernetes e os principais provedores de nuvem (EKS, GKE, AKS) adotam Fluent Bit como agente padrão de coleta de logs

**Descartado** em favor do Fluent Bit por menor footprint e maior flexibilidade.

### OpenTelemetry Collector

**Prós:**
- Padrão aberto e vendor-neutral (CNCF)
- Coleta logs, métricas e traces em um único agente
- Suporte nativo ao protocolo OTLP

**Contras:**
- **Maior complexidade de configuração** para o escopo atual
- **Curva de aprendizado** mais alta para a equipe
- Benefícios são maiores em ambientes com múltiplos backends de observabilidade

**Não descartado para o futuro** — a migração para OpenTelemetry Collector é uma evolução natural se a stack crescer para múltiplos destinos (ex: Datadog, Jaeger, Grafana Cloud). O uso do Fluent Bit não bloqueia essa transição.

---

## Trade-offs Documentados

| Aspecto | Decisão | Trade-off |
|---|---|---|
| Acoplamento das apps | Apps escrevem apenas em stdout | Apps não conhecem o backend de observabilidade — ganho de desacoplamento, mas requer container adicional |
| Resiliência | Buffer em disco no Fluent Bit | Logs sobrevivem a quedas do Elasticsearch, mas há limite de disco configurável |
| Garantia de entrega | At-least-once | Em reconexões pode haver pequenas duplicatas de logs — aceitável para observabilidade |
| Enriquecimento | Feito pelo Fluent Bit (Docker metadata) | Metadados de infraestrutura automáticos, sem poluir o código das aplicações |
| Portabilidade | Fluent Bit é padrão em Kubernetes | Facilita migração futura para K8s sem mudança nas aplicações |

---

## Consequências

**Positivas:**
- As aplicações ficam completamente desacopladas do backend de observabilidade — uma troca de Elasticsearch por outro destino não exige nenhuma mudança no código
- Em caso de indisponibilidade do Elasticsearch, os logs são preservados em disco e entregues automaticamente na recuperação, sem qualquer impacto nas aplicações
- O enriquecimento automático de metadados Docker (nome do container, imagem, labels) melhora a rastreabilidade sem poluir o código das aplicações
- O Fluent Bit é leve (~1MB de footprint), adequado para ambientes locais de desenvolvimento
- A abordagem é alinhada com as boas práticas cloud-native e prepara a solução para uma eventual migração para Kubernetes

**Negativas:**
- Um container adicional precisa ser gerenciado no `docker-compose` e nas pipelines de infraestrutura
- A garantia de entrega é **at-least-once**, não **exactly-once** — pode haver duplicatas de logs em cenários de reconexão (aceitável para observabilidade)
- O buffer em disco tem tamanho configurável e finito — em indisponibilidades prolongadas do Elasticsearch pode haver perda de logs mais antigos se o buffer atingir o limite

---

## Referências

- [Fluent Bit — Official Documentation](https://docs.fluentbit.io/)
- [Fluent Bit — Buffering & Storage](https://docs.fluentbit.io/manual/administration/buffering-and-storage)
- [Fluent Bit — Docker Log Driver](https://docs.fluentbit.io/manual/pipeline/inputs/tail)
- [Elastic — Getting logs into Elasticsearch (recomendações oficiais)](https://www.elastic.co/guide/en/elasticsearch/reference/current/getting-started.html)
- [CNCF Landscape — Logging](https://landscape.cncf.io/card-mode?category=logging&grouping=category)
- [12-Factor App — Logs](https://12factor.net/logs)
