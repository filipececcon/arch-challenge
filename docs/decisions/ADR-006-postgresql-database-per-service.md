# ADR-006 — PostgreSQL com Padrão Database per Service

- **Status:** Aceito
- **Data:** 2026-04-03
- **Decisores:** Time de Arquitetura

---

## Contexto

Os serviços CashFlow e Dashboard precisam persistir dados. Era necessário decidir:

1. Qual banco de dados utilizar
2. Se os serviços compartilhariam um banco de dados ou teriam bancos independentes

A escolha do banco de dados considera os critérios:
- Licenciamento open source (sem custo de licença)
- Suporte a dados relacionais (lançamentos financeiros, consolidados por data)
- Maturidade e suporte da comunidade
- Compatibilidade com Entity Framework Core

O padrão de isolamento de banco de dados é fundamental para garantir a independência operacional entre os serviços, conforme decisão no ADR-002.

---

## Decisão

Utilizar **PostgreSQL** como banco de dados relacional para ambos os serviços, seguindo o padrão **Database per Service** — cada serviço possui seu próprio banco de dados isolado.

### Distribuição dos bancos

| Serviço | Banco | Schemas | Principais tabelas |
|---|---|---|---|
| CashFlow | `cashflow_db` | `public`, `outbox`, `control` | `TB_ACCOUNT`, `TB_TRANSACTION`, `TB_OUTBOX`, `__EFMigrationsHistory` |
| Dashboard | `dashboard_db` | `public` | `daily_consolidations`, `processed_integration_events` |

O banco `cashflow_db` utiliza schemas separados por responsabilidade para permitir controle de acesso granular por role — detalhado em [ADR-015 — Segregação de Schemas PostgreSQL](./ADR-015-segregacao-schemas-postgresql.md).

Em ambiente de desenvolvimento (docker-compose), ambos os bancos podem coexistir na mesma instância PostgreSQL. Em produção, recomenda-se instâncias separadas.

---

## Alternativas Consideradas

### MySQL / MariaDB

**Prós:**
- Open source, sem custo de licença
- Muito utilizado em aplicações web

**Contras:**
- Suporte a tipos avançados (JSON, arrays, UUID nativo) menos robusto que PostgreSQL
- PostgreSQL tem melhor reputação para workloads financeiros por maior consistência ACID
- Comunidade menor no ecossistema .NET

**Descartado** em favor do PostgreSQL por maior robustez e recursos avançados.

### SQL Server

**Prós:**
- Integração nativa com o ecossistema Microsoft/.NET
- Ferramentas de administração maduras

**Contras:**
- Licenciamento proprietário com custo significativo em produção
- Viola o critério de licença open source

**Descartado** por custo de licenciamento.

### MongoDB (banco de documentos)

**Prós:**
- Flexibilidade de schema
- Boa performance para leituras por chave

**Contras:**
- Dados financeiros têm natureza relacional e se beneficiam de schema rígido e transações ACID
- Consistência eventual nativa não é ideal para lançamentos financeiros
- Menor familiaridade no ecossistema .NET enterprise

**Descartado** por não ser adequado para dados financeiros estruturados.

### Banco de dados compartilhado (shared database)

**Prós:**
- Menor overhead operacional
- Joins entre tabelas dos dois serviços possíveis

**Contras:**
- Cria acoplamento entre os serviços a nível de schema
- Uma migração mal executada em um serviço pode afetar o outro
- Viola o princípio de isolamento entre bounded contexts (ADR-002)
- Dificulta escalabilidade independente

**Descartado** por violação do princípio de independência entre serviços.

---

## Consequências

**Positivas:**
- Isolamento total entre os serviços — mudanças de schema em um não afetam o outro
- PostgreSQL oferece transações ACID completas, fundamental para dados financeiros
- Open source sem custo de licença
- Suporte excelente no Entity Framework Core via Npgsql
- Suporte a UUID, JSONB, tipos monetários (numeric/decimal) nativos

**Negativas:**
- Dados dos lançamentos e do consolidado não podem ser acessados via JOIN — a consistência é garantida por eventos (ADR-003)
- Em produção, duas instâncias de banco aumentam o custo e a complexidade operacional
- Backups precisam ser gerenciados separadamente por serviço

---

## Referências

- [Microservices Patterns — Database per Service](https://microservices.io/patterns/data/database-per-service.html)
- [Npgsql — Entity Framework Core Provider for PostgreSQL](https://www.npgsql.org/efcore/)
- [PostgreSQL — ACID Compliance](https://www.postgresql.org/about/)
- [ADR-015 — Segregação de Schemas PostgreSQL por Responsabilidade e Controle de Acesso](./ADR-015-segregacao-schemas-postgresql.md)
