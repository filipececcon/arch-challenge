# ADR-009 — API Gateway com Ocelot

- **Status:** Aceito
- **Data:** 2026-04-05
- **Decisores:** Time de Arquitetura
- **Relacionado a:** [ADR-008 — Autenticação e Autorização com Keycloak](./ADR-008-autenticacao-autorizacao-keycloak.md), [ADR-002 — Separação em Dois Bounded Contexts](./ADR-002-separacao-cashflow-dashboard.md)

---

## Contexto

Com dois serviços backend independentes (CashFlow e Dashboard, internamente na porta 8080 e mapeados para 5001/5002 no host em desenvolvimento), o sistema apresentava os seguintes problemas:

- O frontend Angular precisava conhecer e gerenciar duas origens distintas
- Cada serviço implementava sua própria camada de autenticação JWT de forma duplicada
- Não havia um ponto único para aplicar políticas transversais (rate limiting, CORS, logging de acesso)
- A documentação Swagger estava fragmentada em dois endpoints separados
- Qualquer mudança de segurança (ex: troca de algoritmo JWT, rotação de chaves) precisava ser aplicada em ambos os serviços individualmente

Era necessária uma camada de entrada unificada que centralizasse essas responsabilidades sem acoplar os serviços entre si.

---

## Decisão

Introduzir um **API Gateway** usando **Ocelot** (ASP.NET Core) como ponto único de entrada para todas as requisições dos frontends, centralizando:

1. **Autenticação** — validação do JWT emitido pelo Keycloak (assinatura, issuer, audience, expiração)
2. **Roteamento** — mapeamento de rotas públicas para os serviços downstream
3. **Documentação** — Swagger UI agregado via `MMLib.SwaggerForOcelot`

### Fluxo atualizado

```mermaid
sequenceDiagram
    participant A as Angular App
    participant G as Ocelot Gateway :5000
    participant C as CashFlow API :8080
    participant D as Dashboard API :8080

    A->>G: Bearer <JWT> → GET /cashflow/v1/transactions
    Note over G: 1. Valida JWT contra Keycloak JWKS endpoint
    Note over G: 2. Verifica roles/claims (RouteClaimsRequirement)
    Note over G: 3. Roteia para o serviço downstream correto
    G->>C: GET /api/transactions
    G->>D: GET /api/daily-balances
    C-->>G: Response (lógica de negócio pura)
    D-->>G: Response (lógica de negócio pura)
    G-->>A: Response
```

### Mapeamento de rotas

| Rota no Gateway (upstream) | Serviço Downstream | Path Downstream | Porta interna |
|---|---|---|---|
| `GET /cashflow/v1/transactions` | CashFlow API | `/api/transactions` | 8080 |
| `POST /cashflow/v1/transactions` | CashFlow API | `/api/transactions` | 8080 |
| `GET /dashboard/v1/daily-balances` | Dashboard API | `/api/daily-balances` | 8080 |

> O template `DownstreamPathTemplate: "/api/{everything}"` mapeia qualquer path upstream `/cashflow/v1/*` para `/api/*` no serviço downstream.

### Configuração Ocelot (`ocelot.json`)

```json
{
  "Routes": [
    {
      "UpstreamPathTemplate": "/cashflow/v1/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE" ],
      "DownstreamPathTemplate": "/api/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        { "Host": "cashflow-api", "Port": 8080 }
      ],
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Bearer",
        "AllowedScopes": []
      },
      "RouteClaimsRequirement": {
        "roles": "comerciante,admin"
      }
    },
    {
      "UpstreamPathTemplate": "/dashboard/v1/{everything}",
      "UpstreamHttpMethod": [ "GET" ],
      "DownstreamPathTemplate": "/api/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        { "Host": "dashboard-api", "Port": 8080 }
      ],
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Bearer",
        "AllowedScopes": []
      },
      "RouteClaimsRequirement": {
        "roles": "gestor,admin"
      }
    }
  ],
  "GlobalConfiguration": {
    "BaseUrl": "http://localhost:5000"
  }
}
```

### Swagger agregado (`MMLib.SwaggerForOcelot`)

```json
{
  "SwaggerEndPoints": [
    {
      "Key": "cashflow",
      "Config": [
        {
          "Name": "CashFlow API",
          "Version": "v1",
          "Url": "http://cashflow-api:8080/swagger/v1/swagger.json"
        }
      ]
    },
    {
      "Key": "dashboard",
      "Config": [
        {
          "Name": "Dashboard API",
          "Version": "v1",
          "Url": "http://dashboard-api:8080/swagger/v1/swagger.json"
        }
      ]
    }
  ]
}
```

O Swagger UI unificado fica disponível em `http://localhost:5000/swagger`, com todos os endpoints das duas APIs roteados corretamente.

### Divisão de responsabilidades de segurança

| Camada | Responsabilidade |
|---|---|
| **Ocelot Gateway** | Validação completa do JWT (assinatura, issuer, audience, expiração), verificação de roles via `RouteClaimsRequirement`, CORS, rate limiting |
| **CashFlow API** | Lógica de negócio pura — sem `[Authorize]` |
| **Dashboard API** | Lógica de negócio pura — sem `[Authorize]` |

As APIs downstream **não implementam nenhuma lógica de autenticação ou autorização**. Todo o contexto de segurança é resolvido exclusivamente no Gateway, isolando completamente as APIs de negócio dessa responsabilidade.

Esse modelo é seguro desde que as APIs downstream sejam inacessíveis diretamente de fora da rede interna. Em **desenvolvimento**, as portas `5001:8080` (CashFlow) e `5002:8080` (Dashboard) são publicadas para facilitar testes, mas em **produção** apenas o Gateway na porta 5000 deve ser exposto externamente — as APIs ficam acessíveis somente pela rede interna do cluster.

---

## Alternativas Consideradas

### Segurança em cada API individualmente (sem gateway)

**Prós:**
- Menor número de componentes na infraestrutura
- Cada serviço completamente autônomo sem dependência de componente central

**Contras:**
- Lógica de autenticação duplicada em cada serviço
- Qualquer mudança de política de segurança requer deploy coordenado de múltiplos serviços
- Frontend precisa conhecer múltiplas origens
- Sem ponto único para aplicar rate limiting e CORS de forma consistente
- Swagger fragmentado em dois endpoints distintos

**Descartado** pela duplicação de responsabilidades e dificuldade de evolução da política de segurança.

### NGINX como API Gateway (com Lua ou módulos)

**Prós:**
- Alta performance e amplamente utilizado em produção
- Suporte nativo a TLS termination e load balancing

**Contras:**
- Validação de JWT requer módulos externos ou Lua scripting — complexidade fora do ecossistema .NET
- Sem integração nativa com Swagger para agregação de documentação
- Configuração de autorização por roles é limitada comparada a uma solução programática

**Descartado** por falta de integração com o ecossistema ASP.NET Core e ausência de suporte nativo a Swagger.

### YARP (Yet Another Reverse Proxy)

**Prós:**
- Biblioteca oficial da Microsoft para ASP.NET Core
- Alta performance e suporte a configuração dinâmica

**Contras:**
- Não possui suporte nativo a agregação de Swagger como `MMLib.SwaggerForOcelot`
- Exige mais código de configuração manual para roteamento com autenticação
- Ecossistema de plugins menor que o Ocelot para este cenário específico

**Descartado** em favor do Ocelot pela maturidade do ecossistema e suporte nativo à agregação de Swagger.

---

## Consequências

**Positivas:**
- Ponto único de entrada: frontend conhece apenas `localhost:5000`
- Todo o contexto de segurança (autenticação + autorização) centralizado no Gateway — mudanças de roles/permissões não exigem deploy das APIs de negócio
- APIs downstream são serviços de negócio puros, sem acoplamento a qualquer framework ou política de segurança
- Swagger UI unificado com todas as APIs documentadas em `localhost:5000/swagger`
- CORS e rate limiting configurados uma única vez no gateway

**Negativas:**
- O gateway se torna o único ponto de verificação de segurança — **as APIs downstream não devem ser publicadas externamente em produção** (portas internas 8080, mapeadas 5001/5002 apenas em desenvolvimento)
- Perde-se o _defense in depth_ no nível das APIs: um bypass no gateway expõe os serviços sem nenhuma barreira secundária
- O gateway se torna um componente crítico de infraestrutura — em produção requer alta disponibilidade (múltiplas instâncias + load balancer)
- Uma falha no gateway torna ambas as APIs inacessíveis pelos frontends, mesmo que os serviços downstream estejam saudáveis
- Hop adicional na rede (latência marginal, desprezível para este cenário)

---

## Estrutura do serviço Gateway

```
services/
└── gateway/
    ├── Gateway.csproj
    ├── Program.cs
    ├── ocelot.json
    └── Dockerfile
```

---

## Referências

- [Ocelot — Documentação oficial](https://ocelot.readthedocs.io/)
- [MMLib.SwaggerForOcelot](https://github.com/Moxuanyu/MMLib.SwaggerForOcelot)
- [API Gateway Pattern — microservices.io](https://microservices.io/patterns/apigateway.html)
- [ADR-008 — Autenticação e Autorização com Keycloak](./ADR-008-autenticacao-autorizacao-keycloak.md)
