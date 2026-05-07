# ADR-004 — Backend com ASP.NET Core

- **Status:** Aceito
- **Data:** 2026-04-03
- **Decisores:** Time de Arquitetura

---

## Contexto

Os serviços CashFlow e Dashboard precisam de uma camada de API para expor suas funcionalidades. Era necessário escolher o framework e a linguagem para construção dos backends.

A empresa solicitante do desafio já utiliza ASP.NET Core como parte de sua stack tecnológica, o que torna a escolha estratégica além de técnica — facilita a manutenção, onboarding de novos membros e alinhamento com padrões já estabelecidos.

---

## Decisão

Utilizar **ASP.NET Core** (C#) como framework para os backends de ambos os serviços.

### Versão e padrões adotados

- .NET 8 (LTS — Long Term Support)
- Minimal APIs para endpoints simples, Controllers para endpoints com maior complexidade
- Entity Framework Core para acesso ao PostgreSQL
- MassTransit como abstração sobre o RabbitMQ

### Organização interna dos projetos

Cada backend seguirá a arquitetura em camadas com separação clara de responsabilidades:

```
services/cashflow/
├── ArchChallenge.CashFlow.sln
├── src/
│   ├── Agents/                 ← Workers de background (Outbox)
│   ├── Api/                    ← Apresentação (Controllers, Middlewares)
│   ├── Application/            ← Casos de uso, DTOs, interfaces
│   ├── Caching/                ← CrossCutting: Redis / ITaskCacheService
│   ├── Documents/              ← Infraestrutura: MongoDB (read models)
│   ├── Domain/                 ← Entidades, regras de negócio
│   ├── I18n/                   ← CrossCutting: internacionalização
│   ├── Immutable/              ← Infraestrutura: ImmuDB (auditoria)
│   ├── Logging/                ← CrossCutting: Serilog, base dos workers
│   ├── Messaging/              ← CrossCutting: MassTransit / RabbitMQ
│   ├── Relational/             ← Infraestrutura: EF Core / PostgreSQL
│   ├── Security/               ← CrossCutting: JWT / Keycloak
│   └── Shared/                 ← Domain.Shared: interfaces, enums, entidades base
└── tests/
    ├── Unit/
    └── Integration/
```

(O serviço Dashboard segue o mesmo padrão em `services/dashboard/`, com solution `ArchChallenge.Dashboard.sln`.)

---

## Alternativas Consideradas

### Node.js com NestJS

**Prós:**
- Ecossistema JavaScript unificado com o frontend
- Alta velocidade de I/O assíncrono

**Contras:**
- Fora da stack da empresa solicitante
- Tipagem menos robusta que C# mesmo com TypeScript
- Onboarding mais complexo para times C#/.NET

**Descartado** por não estar alinhado com a stack da empresa.

### Go

**Prós:**
- Alta performance e baixo consumo de memória
- Binários autossuficientes

**Contras:**
- Fora da stack da empresa solicitante
- Menor maturidade de frameworks enterprise (ex: ORM, DI)
- Curva de aprendizado para times .NET

**Descartado** por não estar alinhado com a stack da empresa.

### Java com Spring Boot

**Prós:**
- Ecossistema enterprise maduro
- Grande comunidade

**Contras:**
- Fora da stack da empresa solicitante
- Maior consumo de memória em relação ao .NET
- Configuração mais verbosa

**Descartado** por não estar alinhado com a stack da empresa.

---

## Consequências

**Positivas:**
- Alinhamento direto com a stack da empresa solicitante
- Facilita onboarding e manutenção
- .NET 8 LTS garante suporte e atualizações de segurança até novembro de 2026
- Ecossistema maduro para APIs REST, mensageria e acesso a banco de dados
- Integração nativa com Keycloak via middleware de autenticação JWT (Microsoft.AspNetCore.Authentication.JwtBearer)

**Negativas:**
- Maior consumo de memória em comparação a Go ou Node.js em workloads simples
- Requer licença de IDE (Visual Studio) para desenvolvimento, embora o SDK seja gratuito — mitigado pelo uso do Rider ou VS Code com extensão C#

---

## Referências

- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [MassTransit — RabbitMQ Integration](https://masstransit.io/documentation/transports/rabbitmq)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [.NET 8 LTS Release Notes](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
