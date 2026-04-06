# ADR-008 — Autenticação e Autorização com Keycloak

- **Status:** Aceito (atualizado em 2026-04-05)
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

```
[Usuário]
    |
    | 1. Acessa o frontend (Angular)
    ↓
[Angular App]
    |
    | 2. Redireciona para login (Authorization Code Flow + PKCE)
    ↓
[Keycloak]
    |
    | 3. Autentica e emite JWT (Access Token + Refresh Token)
    ↓
[Angular App]
    |
    | 4. Envia JWT no header Authorization: Bearer <token>
    ↓
[Ocelot API Gateway :5000]
    |
    | 5. Valida JWT contra a chave pública do Keycloak (JWKS endpoint)
    |    (assinatura, issuer, audience, expiração)
    | 6. Roteia para o serviço downstream correspondente
    ↓
[ASP.NET Core API — CashFlow ou Dashboard]
    |
    | 7. Autoriza com base em roles/claims do JWT
    |    (não revalida assinatura — confia no gateway)
```

### Configuração de Realm e Clients

| Recurso | Configuração |
|---|---|
| Realm | `cashflow` |
| Client CashFlow Frontend | `cashflow-frontend` (public client, PKCE) |
| Client Dashboard Frontend | `dashboard-frontend` (public client, PKCE) |
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

### Proteção das APIs (ASP.NET Core)

A validação completa do JWT (assinatura, `iss`, `aud` e expiração) é feita pelo **API Gateway (Ocelot)** antes de a requisição chegar às APIs downstream — ver [ADR-009](./ADR-009-api-gateway-ocelot.md).

As APIs downstream mantêm apenas a autorização por roles/claims:

- Diretiva `[Authorize(Roles = "...")]` para proteger endpoints por papel de usuário
- Policies baseadas em claims do JWT para regras de acesso mais granulares
- **Não revalidam** a assinatura do JWT, pois confiam que o gateway já executou essa verificação

### Proteção do Frontend (Angular)

- Biblioteca `angular-auth-oidc-client` para gerenciar o fluxo OIDC
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
- [angular-auth-oidc-client](https://github.com/damienbod/angular-auth-oidc-client)
- [ASP.NET Core — JWT Bearer Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn)
