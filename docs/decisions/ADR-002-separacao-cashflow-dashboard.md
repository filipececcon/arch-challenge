# ADR-002 — Separação em Dois Bounded Contexts: CashFlow e Dashboard

- **Status:** Aceito
- **Data:** 2026-04-03
- **Decisores:** Time de Arquitetura

---

## Contexto

O desafio técnico descreve dois domínios funcionais distintos:

1. **Controle de lançamentos** — registro de débitos e créditos no fluxo de caixa diário
2. **Consolidado diário** — geração e disponibilização do saldo diário consolidado

Um requisito não funcional crítico determina que o **serviço de controle de lançamentos não deve ficar indisponível caso o serviço de consolidado diário falhe**. Isso indica que os dois domínios devem ser tratados como unidades independentes de falha e implantação.

---

## Decisão

Separar o sistema em **dois bounded contexts distintos**, cada um com seu próprio backend, frontend, banco de dados e ciclo de vida de deploy:

- **CashFlow** — responsável pelo registro e gestão de lançamentos financeiros (débitos e créditos)
- **Dashboard** — responsável pelo processamento e exibição do consolidado diário

---

## Responsabilidades

### CashFlow

| Componente | Responsabilidade |
|---|---|
| Backend (ASP.NET Core) | API REST para criação, listagem e cancelamento de lançamentos |
| Frontend (Angular) | Interface para o comerciante registrar débitos e créditos |
| Banco de dados (PostgreSQL) | Persistência dos lançamentos |
| Publicação de eventos | Emite evento `LancamentoRegistrado` no RabbitMQ |

### Dashboard

| Componente | Responsabilidade |
|---|---|
| Backend (ASP.NET Core) | Consome eventos e expõe API do consolidado diário |
| Frontend (Angular) | Interface para visualização do saldo e relatórios diários |
| Banco de dados (PostgreSQL) | Persistência do consolidado calculado |
| Consumo de eventos | Processa evento `LancamentoRegistrado` do RabbitMQ |

---

## Alternativas Consideradas

### Sistema monolítico único

**Prós:**
- Menor complexidade operacional inicial
- Transações distribuídas desnecessárias

**Contras:**
- Viola o requisito não funcional: uma falha em qualquer módulo afetaria o sistema inteiro
- Dificulta escalabilidade independente (ex: Dashboard sob pico de 50 req/s precisaria escalar todo o sistema)
- Menor clareza de responsabilidades e limites de domínio

### Três ou mais serviços

**Prós:**
- Maior granularidade de deploy e escalonamento

**Contras:**
- Complexidade operacional desnecessária para o escopo atual
- O desafio define explicitamente dois serviços como requisito de negócio

---

## Consequências

**Positivas:**
- Isolamento de falhas: CashFlow continua operando mesmo que o Dashboard esteja indisponível
- Escalabilidade independente: Dashboard pode ser escalado horizontalmente para suportar picos de 50 req/s
- Clareza de ownership e responsabilidades por domínio
- Deploys independentes sem coordenação entre times

**Negativas:**
- Necessidade de comunicação assíncrona entre os serviços (abordada no ADR-003)
- Consistência eventual entre os dados de lançamento e o consolidado
- Maior complexidade de infraestrutura em relação a um monolito

---

## Referências

- [Domain-Driven Design — Eric Evans](https://www.domainlanguage.com/ddd/)
- [Microservices Patterns — Chris Richardson](https://microservices.io/patterns/)
- [The Independent Deployability Principle](https://microservices.io/patterns/decomposition/decompose-by-subdomain.html)
