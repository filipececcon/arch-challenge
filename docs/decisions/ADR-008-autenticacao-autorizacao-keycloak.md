# ADR-008 — Autenticação e Autorização com Keycloak

- **Status:** Aceito (atualizado em 2026-04-10)
- **Data:** 2026-04-03
- **Decisores:** Time de Arquitetura
- **Relacionado a:** [ADR-009 — API Gateway com Ocelot](./ADR-009-api-gateway-ocelot.md)

---

## Contexto

O sistema precisa de uma estratégia de autenticação e autorização para proteger as APIs do CashFlow e do Dashboard, bem como os frontends Angular.

Os requisitos de segurança obrigatórios do desafio incluem:

- Autenticação de usuários
- Autorização por papel/permissão
- Proteção de APIs
- Controle de acesso entre serviços
- Proteção de dados sensíveis

Era necessário escolher uma solução de Identity Provider (IdP) que atendesse a esses requisitos sem custo de licenciamento.

---

## Decisão

Utilizar **Keycloak** como Identity Provider central para autenticação e autorização de toda a solução, implementando o protocolo **OAuth 2.0 / OpenID Connect (OIDC)**.

### Fluxo de autenticação

> **Nota:** A partir do ADR-009, a validação do JWT é feita pelo **API Gateway (Ocelot)** antes de as requisições chegarem às APIs downstream. O fluxo abaixo reflete essa camada adicional.

```mermaid
sequenceDiagram
    actor U as Usuário
    participant A as Angular App
    participant K as Keycloak
    participant G as Ocelot API Gateway :5000
    participant API as ASP.NET Core API<br/>(CashFlow ou Dashboard)

    U->>A: 1. Acessa o frontend
    A->>K: 2. Redireciona para login<br/>(Authorization Code Flow + PKCE)
    K-->>A: 3. Autentica e emite JWT<br/>(Access Token + Refresh Token)
    A->>G: 4. Envia JWT no header<br/>Authorization: Bearer token
    Note over G: 5. Valida JWT: assinatura RSA,<br/>issuer, audience, expiração
    Note over G: 6. Verifica roles via<br/>RouteClaimsRequirement<br/>(comerciante, admin, gestor)
    G->>API: 7. Encaminha requisição com<br/>header Authorization intacto
    Note over API: 8. Revalida JWT independentemente:<br/>assinatura, issuer, audience, expiração<br/>(defense in depth — camada autônoma)
    Note over API: 9. [Authorize] garante token válido;<br/>roles NÃO são verificadas aqui<br/>(fonte única de verdade = Gateway)
```

### Configuração de Realm e Clients

| Recurso | Configuração |
|---|---|
| Realm | `cashflow` |
| Client Frontend (SPA unificada) | `cashflow-frontend` (public client, PKCE) — único client para a SPA Angular |
| Client CashFlow API | `cashflow-api` (confidential, para M2M se necessário) |
| Client Dashboard API | `dashboard-api` (confidential, para M2M se necessário) |

### Roles definidas

| Role | Permissões |
|---|---|
| `comerciante` | Registrar lançamentos, visualizar próprios lançamentos |
| `gestor` | Visualizar consolidado diário, relatórios |
| `admin` | Acesso completo |

---

## Alternativas Consideradas

### Auth0

**Prós:**
- SaaS gerenciado, sem overhead operacional
- UI de administração excelente
- Suporte enterprise robusto

**Contras:**
- Custo de licenciamento significativo em produção (model freemium com limites)
- Dados de identidade em infraestrutura de terceiros (privacidade e compliance)
- Vendor lock-in

**Descartado** por custo de licenciamento.

### Azure Active Directory B2C

**Prós:**
- Integrado ao ecossistema Microsoft
- SaaS gerenciado

**Contras:**
- Custo de licença por usuário ativo
- Configuração mais complexa para cenários simples
- Vendor lock-in no Azure

**Descartado** por custo de licenciamento e vendor lock-in.

### Implementação própria (JWT manual)

**Prós:**
- Total controle
- Sem dependência de componentes externos

**Contras:**
- Alto risco de falhas de segurança por implementação incorreta
- Enorme esforço de desenvolvimento (login, refresh token, logout, revogação, MFA)
- Reinventar a roda em uma área crítica de segurança

**Descartado** por risco de segurança inaceitável.

---

## Estratégia de Segurança Complementar

### Proteção das APIs (ASP.NET Core) — Defense in Depth

A arquitetura implementa duas camadas independentes de segurança:

**Camada 1 — API Gateway (Ocelot):** valida o JWT (assinatura, `iss`, `aud`, `exp`) e verifica as roles exigidas por rota via `RouteClaimsRequirement`. É a fonte única de verdade da **política de autorização** (quem pode acessar o quê).

**Camada 2 — APIs downstream (CashFlow):** revalidam o JWT de forma autônoma via `AddJwtBearer`, sem depender do gateway. Aplicam `[Authorize]` no controller para garantir que toda requisição carrega um token válido. **Não verificam roles** — essa responsabilidade pertence exclusivamente ao Gateway, evitando acoplamento duplicado de política de acesso.

Essa separação garante que um acesso direto à rede interna (bypass do gateway por comprometimento de container ou misconfiguration de Network Policy) ainda seja bloqueado com `401 Unauthorized` pela API. Ver [authorization.md](../security/authorization.md) para detalhes da implementação e justificativa.

### Proteção do Frontend (Angular)

- Implementação manual de OIDC/PKCE (sem biblioteca de terceiros) — serviços em `services/frontend/src/app/core/auth/`
- Tokens armazenados em memória (não em localStorage) para prevenir XSS
- Route guards para proteger rotas autenticadas
- Interceptor HTTP para injeção automática do Bearer token

### Comunicação entre serviços (M2M)

- Fluxo **Client Credentials** do OAuth 2.0 para comunicações machine-to-machine futuras
- Cada serviço possui seu próprio client confidencial no Keycloak

---

## Consequências

**Positivas:**
- Open source, sem custo de licença (Keycloak é mantido pela Red Hat/Quarkus)
- Suporte nativo a OAuth 2.0, OIDC, SAML, MFA, social login
- Single Sign-On (SSO) entre CashFlow e Dashboard automaticamente
- Gerenciamento centralizado de usuários, roles e sessões
- Tokens JWT stateless — APIs não precisam consultar banco para validar autenticação

**Negativas:**
- Requer operação do Keycloak como componente adicional de infraestrutura
- Curva de configuração inicial maior que soluções SaaS
- Em produção, precisa de alta disponibilidade do Keycloak (cluster ou backup)

---

## Referências

- [Keycloak Documentation](https://www.keycloak.org/documentation)
- [OAuth 2.0 — Authorization Code Flow with PKCE](https://oauth.net/2/pkce/)
- [ASP.NET Core — JWT Bearer Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn)
