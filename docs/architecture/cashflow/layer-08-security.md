# Camada Infrastructure.CrossCutting.Security — ArchChallenge.CashFlow.Infrastructure.CrossCutting.Security

O projeto **ArchChallenge.CashFlow.Infrastructure.CrossCutting.Security** centraliza a configuração de autenticação e autorização do serviço Cashflow: integração com **JWT Bearer** e **Keycloak** em ambientes produtivos, transformação de claims de papéis do realm para `ClaimTypes.Role`, e um modo **local** sem JWT para desenvolvimento e testes.

---

## Responsabilidades

- **Autenticação JWT Bearer via Keycloak** quando a segurança está habilitada: validação de issuer, audience e tempo de vida do token, com descoberta OIDC a partir da authority configurada.
- **Transformação de claims**: mapear roles presentes no claim `realm_access.roles` (JSON emitido pelo Keycloak) para claims `ClaimTypes.Role` no `ClaimsPrincipal`, permitindo autorização baseada em papéis no ASP.NET Core.
- **Modo local para desenvolvimento**: quando JWT está desativado, um handler de autenticação aceita qualquer requisição como autenticada, evitando dependência de Keycloak em máquinas locais (não usar em produção).
- **Autorização declarativa**: uso de `[Authorize]` nos controllers que exigem usuário autenticado; registro de `AddAuthorization()` na composição de serviços.

---

## Modos de autenticação

| Modo | Condição | Mecanismo | Uso |
|------|----------|-----------|-----|
| JWT Bearer (Keycloak) | `Security:Disabled = false` (padrão) | `JwtBearerDefaults.AuthenticationScheme` + validação de issuer, audience, lifetime | Produção |
| Local | `Security:Disabled = true` | `LocalAuthenticationHandler` aceita qualquer request | Desenvolvimento / testes locais |

---

## Diagrama de Classes

Visão estática dos componentes de segurança e do ponto de extensão na injeção de dependências.

```mermaid
classDiagram
    class AuthenticationHandler~AuthenticationSchemeOptions~ {
        <<abstract>>
    }

    class LocalAuthenticationHandler {
    }

    class IClaimsTransformation {
        <<interface>>
    }

    class KeycloakRolesClaimsTransformation {
    }

    class DependencyInjection {
        +AddSecurityConfiguration(services, configuration)$ IServiceCollection
    }

    AuthenticationHandler~AuthenticationSchemeOptions~ <|-- LocalAuthenticationHandler
    IClaimsTransformation <|.. KeycloakRolesClaimsTransformation

    note for DependencyInjection "AddSecurityConfiguration: se Security:Disabled = true registra LocalAuthenticationHandler; caso contrário JWT Bearer + KeycloakRolesClaimsTransformation."
```

**Notas:**

- Quando **`Security:Disabled`** é verdadeiro, não há validação JWT: o esquema local autentica todas as requisições para facilitar desenvolvimento.
- Quando é falso, o pipeline usa **JWT Bearer** e, após validação do token, **`KeycloakRolesClaimsTransformation`** enriquece o principal com roles derivadas de `realm_access`.

---

## Diagrama de Sequência — Autenticação JWT (produção)

Fluxo típico de uma chamada autenticada a um endpoint protegido (por exemplo, transações).

```mermaid
sequenceDiagram
    participant Cliente
    participant Gateway as API Gateway
    participant Pipeline as Pipeline ASP.NET Core
    participant JwtBearer as JwtBearerHandler
    participant Transform as KeycloakRolesClaimsTransformation
    participant Controller as TransactionsController

    Cliente->>Gateway: HTTP + Authorization: Bearer {token}
    Gateway->>Pipeline: encaminha (rota ex.: /api/transactions)
    Note over Pipeline,Controller: Autenticação e autorização executam antes do action

    Pipeline->>JwtBearer: valida token (assinatura, issuer, audience, lifetime)
    alt Token válido
        JwtBearer->>Transform: TransformAsync(Principal)
        Transform->>Transform: extrai roles de realm_access.roles
        Transform-->>JwtBearer: ClaimsPrincipal com ClaimTypes.Role
        JwtBearer-->>Pipeline: usuário autenticado
        Pipeline->>Controller: [Authorize] satisfeito — executa action
    else Token inválido ou ausente
        JwtBearer-->>Pipeline: falha de autenticação
        Pipeline-->>Cliente: 401 Unauthorized
    end
```

---

## Configuração

| Chave | Descrição | Exemplo |
|-------|-----------|---------|
| `Security:Disabled` | Desativa JWT (dev/local) | `true` |
| `Keycloak:Authority` | URL do realm Keycloak | `https://keycloak:8080/realms/cashflow` |
| `Keycloak:Audience` | Audience esperado no token | `cashflow-api` |
| `Keycloak:RequireHttpsMetadata` | Exige HTTPS para OIDC discovery | `true` |

O audience padrão no código, quando não informado na configuração, é **`cashflow-api`**.

---

## Endpoints e proteção

| Endpoint | Autenticado | Observação |
|----------|-------------|------------|
| `POST /api/transactions` | Sim | `[Authorize]` no controller |
| `GET /api/transactions/{id}` | Sim | `[Authorize]` no controller |
| `GET /api/transactions` | Sim | `[Authorize]` no controller |
| `GET /api/tasks/{taskId}` | Não | SSE — polling público por taskId opaco |
| `GET /metrics` | Não | Prometheus — protegido por rede/infra |
| `GET /health` | Não | Health checks — protegido por rede/infra |

---

## Decisões

A escolha de **Keycloak** como provedor de identidade e o modelo de **JWT Bearer** para a API estão registrados em [ADR-008 — Autenticação e autorização com Keycloak](../../decisions/ADR-008-autenticacao-autorizacao-keycloak.md).

O endpoint **`GET /api/tasks/{taskId}`** permanece **sem** `[Authorize]` de propósito: o **`taskId`** é um identificador opaco (por exemplo UUID) sem expor dados de negócio na URL; o cliente precisa poder abrir o stream SSE para acompanhar o processamento assim que recebe o identificador no **202 Accepted**, inclusive em cenários em que o token ainda não está disponível ou renovado no cliente. A superfície exposta por esse endpoint é limitada ao status da tarefa associada ao id; proteções adicionais podem ser feitas em rede (gateway, listas de permissão) conforme a política da organização.
