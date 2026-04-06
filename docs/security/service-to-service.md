# Comunicação entre Serviços — M2M, Isolamento de Rede e Client Credentials

## Visão geral

Em uma arquitetura de microsserviços, o controle de acesso não se aplica apenas a usuários humanos — os próprios serviços precisam se autenticar uns com os outros. Este documento descreve como o isolamento de rede e a autenticação machine-to-machine (M2M) são gerenciados no sistema.

---

## Topologia de rede

Todos os serviços rodam na mesma rede Docker interna (`cashflow-network`). A segurança de rede é garantida por **não publicar as portas dos serviços downstream externamente**:

```mermaid
graph TD
    Internet(["🌐 Internet"])

    Internet -->|":5000"| GW
    Internet -->|":8080"| KC
    Internet -->|":15672 — dev"| RMGMT
    Internet -->|":5601 — dev"| KIB
    Internet -->|":3000 — dev"| GRF

    subgraph NET["cashflow-network — rede Docker interna"]
        GW["API Gateway :5000"]
        KC["Keycloak :8080"]
        CF["CashFlow API :8080"]
        DA["Dashboard API :8080"]
        PG[("PostgreSQL :5432")]
        RMQ["RabbitMQ :5672"]
        ES[("Elasticsearch :9200")]
        FB["Fluent Bit"]
        APMS["APM Server"]
        KIB["Kibana :5601"]
        GRF["Grafana :3000"]
        RMGMT["RabbitMQ Mgmt :15672"]

        GW --> CF
        GW --> DA
        GW --> KC

        CF -->|"publish"| RMQ
        DA -->|"consume"| RMQ

        CF --> PG
        DA --> PG
        KC --> PG

        FB --> ES
        KIB --> ES
        APMS --> ES
    end
```

**Portas publicadas externamente** (mapeadas no `docker-compose.yml`):

| Serviço | Porta Externa | Porta Interna | Motivo |
|---|---|---|---|
| Gateway | 5000 | 5000 | Único ponto de entrada das APIs |
| Keycloak | 8080 | 8080 | UI de administração e OIDC discovery |
| RabbitMQ Management | 15672 | 15672 | UI de administração (apenas dev) |
| Kibana | 5601 | 5601 | Visualização de logs (apenas dev) |
| Grafana | 3000 | 3000 | Dashboards (apenas dev) |

**Portas internas APENAS** (não publicadas externamente):

| Serviço | Porta Interna | Motivo |
|---|---|---|
| CashFlow API | 8080 | Acessível apenas pelo Gateway e pela rede interna |
| Dashboard API | 8080 | Acessível apenas pelo Gateway e pela rede interna |
| PostgreSQL | 5432 | Acessível apenas pelos serviços que precisam de banco |
| Elasticsearch | 9200 | Acessível apenas pelo Fluent Bit, Kibana e APM Server |
| RabbitMQ AMQP | 5672 | Acessível apenas pelos produtores e consumidores |

> **Importante:** Em produção, as portas de administração (RabbitMQ Management, Kibana, Grafana) também devem ficar atrás de autenticação ou VPN, não expostas publicamente.

---

## Comunicação atual: CashFlow → Dashboard via RabbitMQ

A comunicação entre os dois serviços de negócio é **assíncrona via RabbitMQ** — os serviços não se chamam diretamente via HTTP.

```mermaid
graph TD
    CF["CashFlow API<br/>(cashflow-service)"]

    subgraph MQ["RabbitMQ :5672"]
        EX["Exchange: cashflow.events"]
        Q["Queue: dashboard.lancamento.registrado<br/>durable · ack manual · DLQ"]
    end

    DA["Dashboard API<br/>(dashboard-service)"]

    CF -->|"publish(LancamentoRegistrado)<br/>amqp://cashflow-service@rabbitmq:5672"| EX
    EX -->|"routing key: lancamento.registrado"| Q
    Q -->|"consume"| DA
    DA -.->|"ack (após processamento bem-sucedido)"| Q
```

### Autenticação no RabbitMQ

Cada serviço usa credenciais dedicadas para conexão ao RabbitMQ — não compartilham o mesmo usuário:

| Serviço | Usuário RabbitMQ | Permissões |
|---|---|---|
| CashFlow API | `cashflow-service` | Publish na exchange `cashflow.events` |
| Dashboard API | `dashboard-service` | Consume na queue `dashboard.lancamento.registrado` |

Em produção, essas credenciais são injetadas via secrets (não variáveis de ambiente em texto claro).

### Resiliência da fila

- Filas e mensagens configuradas como **durable** (persistência em disco)
- Consumo com **ack manual** — mensagem só é removida da fila após processamento bem-sucedido
- **Dead Letter Queue (DLQ)** para mensagens que falham repetidamente (não processadas após N tentativas)

---

## Comunicação M2M futura: Client Credentials Flow

Caso futuras integrações entre serviços precisem de comunicação HTTP síncrona (ex: um novo serviço de relatórios que consulta diretamente o Dashboard), o fluxo **Client Credentials** do OAuth 2.0 deve ser utilizado.

### Por que não reutilizar o token do usuário

Um serviço chamando outro serviço não representa um usuário — é uma comunicação entre sistemas. Repassar o token do usuário original seria:
- **Inseguro:** O serviço downstream não pode confiar que o token foi emitido para ele
- **Incorreto semanticamente:** O token representa um usuário, não um serviço

### Fluxo Client Credentials

> Diagrama de sequência completo: [`diagrams/client-credentials-m2m.mmd`](./diagrams/client-credentials-m2m.mmd)

```mermaid
sequenceDiagram
    autonumber
    participant S as Serviço Cliente
    participant K as Keycloak
    participant G as Ocelot Gateway
    participant D as Dashboard API

    S->>K: POST /token — grant_type=client_credentials
    K->>S: access_token (sub = "relatorios-api", roles = ["gestor"])
    S->>G: GET /dashboard/v1/consolidate — Bearer <access_token>
    G->>G: Valida JWT + verifica role "gestor"
    G->>D: Roteia requisição
    D->>S: 200 OK + dados do consolidado
```

### Configuração dos clients M2M no Keycloak

```
Client: relatorios-api
  Access Type: confidential
  Standard Flow Enabled: false
  Direct Access Grants Enabled: false
  Service Accounts Enabled: true     ← habilita Client Credentials
  
Service Account Roles:
  - gestor    ← apenas o necessário (princípio do mínimo privilégio)
```

### Princípio do mínimo privilégio

Cada serviço recebe apenas as permissões mínimas necessárias para sua função:

| Serviço | Roles M2M | Justificativa |
|---|---|---|
| `cashflow-api` | Nenhuma (apenas publica eventos) | Não precisa consumir outros serviços |
| `dashboard-api` | Nenhuma (apenas consome eventos) | Não precisa consumir outros serviços |
| `relatorios-api` (futuro) | `gestor` | Precisa ler consolidado diário |

---

## Propagação de contexto de usuário (User Context)

Quando o Gateway roteia uma requisição para uma API downstream, o contexto do usuário autenticado pode ser propagado via headers customizados:

```mermaid
sequenceDiagram
    participant C as Cliente Externo
    participant G as Ocelot Gateway
    participant A as CashFlow API

    C->>G: GET /cashflow/v1/... + Authorization: Bearer &lt;JWT&gt;
    Note over G: Valida JWT (assinatura, expiração)<br/>Extrai claims: sub, roles
    G->>A: Requisição roteada + headers propagados
    Note right of G: X-User-Id: &lt;sub do JWT&gt;<br/>X-User-Roles: comerciante<br/>X-Correlation-Id: &lt;uuid gerado no gateway&gt;
    Note over A: Lê X-User-Id → associa lançamento ao usuário<br/>Lê X-Correlation-Id → correlaciona logs<br/>NÃO revalida o token — confia nos headers do Gateway
    A-->>G: Resposta
    G-->>C: Resposta
```

> **Segurança:** Esses headers só devem ser aceitos pelas APIs quando vierem da rede Docker interna. Um cliente externo que tente injetar `X-User-Id` no header será bloqueado, pois a requisição passa pelo Gateway antes de chegar às APIs — e o Gateway sobrescreve esses headers com os valores do JWT validado.

### Configuração no Ocelot para propagação de claims

```json
{
  "AddHeadersToRequest": {
    "X-User-Id":    "Claims[sub] > value",
    "X-User-Roles": "Claims[roles] > value"
  }
}
```

---

## Referências

- [OAuth 2.0 — Client Credentials Grant (RFC 6749)](https://tools.ietf.org/html/rfc6749#section-4.4)
- [Keycloak — Service Accounts](https://www.keycloak.org/docs/latest/server_admin/#_service_accounts)
- [Ocelot — Headers Transformation](https://ocelot.readthedocs.io/en/latest/features/headerstransformation.html)
- [RabbitMQ — Access Control](https://www.rabbitmq.com/access-control.html)
- [OWASP — Microservices Security Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Microservices_Security_Cheat_Sheet.html)
