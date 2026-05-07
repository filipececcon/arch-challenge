# ADR-016 — ImmuDB como armazenamento imutável para auditoria

- **Status:** Aceito
- **Data:** 2026-04-16
- **Decisores:** Time de Arquitetura

---

## Contexto

O bounded context **CashFlow** precisa de uma **trilha de auditoria** que permita demonstrar *quem* fez *o quê* e *quando* sobre agregados financeiros, com foco em:

1. **Imutabilidade lógica** — registros de auditoria não devem ser alterados após o commit sem deixar rastro verificável.
2. **Não-repúdio** — resistência a negar que um evento ocorreu, com base em evidência técnica além de “apenas uma linha em tabela”.
3. **Desacoplamento da API** — falha ou indisponibilidade do armazenamento de auditoria não deve bloquear o processamento do comando de negócio já aceito (desenho *at-least-once* via outbox).
4. **Alinhamento ao stack** — solução operável em container (compose), com cliente adequado ao ecossistema .NET (gRPC).

A auditoria **transacional** (garantir que todo commit de negócio gere intenção de auditoria) permanece no **PostgreSQL**, reutilizando o padrão **transactional outbox** já adotado para outras integrações ([ADR-003](./ADR-003-comunicacao-assincrona-rabbitmq.md)). Faltava decidir **onde** materializar a cópia **imutável e verificável** do evento de auditoria.

---

## Decisão

Adotar **ImmuDB** como **base de dados imutável** dedicada à **persistência verificável** dos registros de auditoria do CashFlow.

### Comportamento acordado

| Aspecto | Escolha |
|---------|---------|
| **Modelo** | Armazenamento **append-only** com cadeia verificável; escrita via SQL API do cliente (`SQLExec` para DDL e INSERT, com tabelas por tipo de agregado `TB_AUDIT_{AGGREGATE_TYPE}`). |
| **Integração** | **Não** na mesma transação OLTP do PostgreSQL: um **worker** (`AuditOutboxWorkerService` no projeto `Agents.Outbox`) lê `TB_OUTBOX` (com `DS_TARGET = 'Audit'`), grava no ImmuDB e marca processamento — padrão outbox, análogo ao espírito do [ADR-003](./ADR-003-comunicacao-assincrona-rabbitmq.md). |
| **Chaves** | Identificadores determinísticos (ex.: prefixo `audit:{uuid}`) para **idempotência** em reprocessamento. |
| **API principal** | Serviço **cashflow-api** não conecta ao ImmuDB diretamente; apenas o processo de *outbox-audit* e o projeto de infraestrutura **Immutable** interagem com o ImmuDB. |

A implementação detalhada — componentes, fluxos, payload e configuração — está em [layer-09-immutable.md](../architecture/cashflow/layer-09-immutable.md) e na visão agregada de dados em [data/README.md](../data/README.md).

---

## Alternativas Consideradas

### Tabela (ou schema) de auditoria apenas em PostgreSQL

**Prós:** simplicidade operacional, mesma ferramenta do OLTP, transações ACID únicas.

**Contras:** dados permanecem **mutáveis** por administrador ou aplicações com permissão elevada; não há garantia criptográfica nativa de integridade ao longo do tempo; não atende ao critério explícito de **verificabilidade** alinhada a trilha append-only.

**Descartado** como destino final da trilha imutável (o PostgreSQL continua a armazenar o **outbox** e o estado transacional).

### Log imutável apenas em arquivo / object storage (WORM)

**Prós:** modelos de retenção e políticas de imutabilidade em alguns provedores.

**Contras:** consistência, consulta por chave, verificação integrada e operação como “banco” exigem mais engenharia customizada; menos coeso com padrão de *verified* do ImmuDB para o mesmo objetivo.

**Descartado** por custo de customização frente a um motor especializado.

### QLDB (AWS) ou serviço gerenciado equivalente

**Prós:** ledger gerenciado, auditoria com mercado maduro na nuvem AWS.

**Contras:** **lock-in** de nuvem, custo e desenho alinhados a ambientes que já adotam AWS como padrão; o repositório prioriza **open source** e execução em compose local.

**Descartado** por aderência ao critério de portabilidade e open source.

### Outros bancos imutáveis / ledger open source

**Prós:** alternativas existem no mercado.

**Contras:** ImmuDB combina **open source**, modelo **key-value** simples para entradas de auditoria, **gRPC** compatível com .NET, documentação e operação em **container** (`codenotary/immudb`).

**ImmuDB foi preferido** após ponderação de maturidade no ecossistema, simplicidade do caso de uso (registro verificável por chave) e encaixe com o worker de outbox já previsto.

---

## Consequências

**Positivas:**

- Trilha de auditoria com **integridade verificável** adequada a cenários de não-repúdio técnico.
- **Separação clara** entre OLTP (PostgreSQL) e loja imutável (ImmuDB), mantendo a API responsiva quando o ImmuDB está indisponível (filas no outbox).
- **Idempotência** de escrita no destino imutável alinhada a reprocessamentos do worker.
- Alinhamento ao [ADR-006](./ADR-006-postgresql-database-per-service.md) (PostgreSQL como serviço de dados do CashFlow) sem confundir **auditoria de longo prazo** com o mesmo cluster OLTP, se desejado segregação operacional futura.

**Negativas:**

- **Mais um datastore** para provisionar, monitorar e fazer backup (ver métricas Prometheus e compose na documentação da camada imutável).
- Curva de aprendizado da equipe em **ImmuDB** (modelo de cliente, SQL API, operações de verificação).
- Em cenários extremos de falha prolongada do ImmuDB, eventos permanecem no PostgreSQL até processamento — exige **alertas** e runbook (coerente com visão de observabilidade do projeto).

---

## Referências

- [ImmuDB — documentação oficial](https://docs.immudb.io/)
- [Microservices.io — Transactional Outbox](https://microservices.io/patterns/data/transactional-outbox.html)
- [layer-09-immutable.md — Camada imutável (auditoria)](../architecture/cashflow/layer-09-immutable.md)
- [data/README.md — Visão por capacidade: dados relacionais, documentos e imutáveis](../data/README.md)
- [ADR-003 — Comunicação assíncrona com RabbitMQ](./ADR-003-comunicacao-assincrona-rabbitmq.md)
- [ADR-006 — PostgreSQL com padrão Database per Service](./ADR-006-postgresql-database-per-service.md)
