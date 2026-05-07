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
        MG[("MongoDB :27017")]
        RDS["Redis :6379"]
        RMQ["RabbitMQ :5672"]
        ES[("Elasticsearch :9200")]
        APMS["APM Server :8200"]
        KIB["Kibana :5601"]
        GRF["Grafana :3000"]
        RMGMT["RabbitMQ Mgmt :15672"]

        GW --> CF
        GW --> DA
        GW --> KC

        CF -->|"publish"| RMQ
        DA -->|"consume"| RMQ

        CF --> PG
        CF --> MG
        CF --> RDS
        DA --> PG
        KC --> PG

        APMS --> ES
        KIB --> ES
    end
```

**Portas publicadas externamente** (mapeadas no `docker-compose.yml`):

| Serviço | Porta Externa | Porta Interna | Motivo |
|---|---|---|---|
| Gateway | 5000 | 5000 | Único ponto de entrada das APIs |
| Keycloak | 8080 | 8080 | UI de administração e OIDC discovery |
| CashFlow API | 5001 | 8080 | Acesso direto para testes em desenvolvimento |
| Dashboard API | 5002 | 8080 | Acesso direto para testes em desenvolvimento |
| RabbitMQ Management | 15672 | 15672 | UI de administração (apenas dev) |
| Kibana | 5601 | 5601 | Visualização de logs (apenas dev) |
| Grafana | 3000 | 3000 | Dashboards (apenas dev) |
| Elasticsearch | 9200 | 9200 | API REST (apenas dev) |
| Prometheus | 9090 | 9090 | UI de consulta (apenas dev) |

> **Importante:** Em **produção**, as portas das APIs (5001/5002), banco de dados, filas e observabilidade **não devem ser publicadas**. Apenas o Gateway (5000) e o Keycloak (8080) devem ser expostos externamente — preferencialmente atrás de um Load Balancer com TLS.

**Portas internas APENAS** (não publicadas mesmo em dev):

| Serviço | Porta Interna | Motivo |
|---|---|---|
| PostgreSQL | 5432 | Acessível apenas pelos serviços que precisam de banco |
| MongoDB | 27017 | Acessível apenas pela CashFlow API |
| Redis | 6379 | Acessível apenas pela CashFlow API |
| RabbitMQ AMQP | 5672 | Acessível apenas pelos produtores e consumidores |

> **Importante:** Em produção, as portas de administração (RabbitMQ Management, Kibana, Grafana) também devem ficar atrás de autenticação ou VPN, não expostas publicamente.

---

## Comunicação atual: CashFlow → Dashboard via RabbitMQ

A comunicação entre os dois serviços de negócio é **assíncrona via RabbitMQ** — os serviços não se chamam diretamente via HTTP.

```mermaid
graph TD
    CF["CashFlow API<br/>(cashflow-service)"]

    subgraph MQ["RabbitMQ :5672"]
        EX["Exchange: cashflow.events (Topic)"]
        Q["Queue: dashboard.transaction.processed<br/>durable · ack manual · DLQ"]
    end

    DA["Dashboard API<br/>(dashboard-service)"]

    CF -->|"publish(TransactionRegisteredIntegrationEvent)<br/>amqp://rabbit@rabbitmq:5672"| EX
    EX -->|"routing key: # (wildcard)"| Q
    Q -->|"consume"| DA
    DA -.->|"ack (após processamento bem-sucedido)"| Q
```

### Autenticação no RabbitMQ

Em desenvolvimento, ambos os serviços usam as mesmas credenciais padrão do container RabbitMQ (`rabbit` / `rabbit`), injetadas via variáveis de ambiente no `docker-compose.yml`. Em produção, as credenciais devem ser diferenciadas por serviço e injetadas via secrets:

| Ambiente | Usuário RabbitMQ | Como injetar |
|---|---|---|
| Desenvolvimento | `rabbit` | Variável de ambiente no Compose |
| Produção (recomendado) | Usuário dedicado por serviço | Kubernetes Secrets / Vault |

Em produção, recomenda-se criar usuários dedicados `cashflow-service` e `dashboard-service` no RabbitMQ com permissões mínimas (publish / consume respectivamente), injetados via secrets gerenciados.

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

```mermaid
sequenceDiagram
    autonumber
    participant S as Serviço Cliente<br/>(ex: relatorios-api)
    participant K as Keycloak<br/>(realm: cashflow)
    participant G as Ocelot Gateway
    participant D as Dashboard API
    Note over S,D: Comunicação machine-to-machine (sem usuário humano)
    S->>K: POST /protocol/openid-connect/token<br/>grant_type=client_credentials<br/>&client_id=relatorios-api<br/>&client_secret=<secret>
    K->>K: Valida client_id e client_secret<br/>Verifica Service Account ativo<br/>Resolve roles do Service Account
    K->>S: access_token (JWT)<br/>sub = "relatorios-api"<br/>realm_access.roles = ["gestor"]
    Note over S: Token armazenado em memória<br/>Renovado antes de expirar
    S->>G: GET /dashboard/v1/daily-balances<br/>Authorization: Bearer <access_token>
    G->>G: Valida JWT<br/>Executa KeycloakRolesClaimsTransformation<br/>Verifica RouteClaimsRequirement:<br/>rota /dashboard/v1/** exige "gestor" ou "admin"<br/>✅ Service Account tem role "gestor"
    G->>D: Proxy HTTP downstream
    D->>G: 200 OK + consolidado
    G->>S: 200 OK + dados
    Note over S,D: Princípio do mínimo privilégio:<br/>relatorios-api tem apenas role "gestor"<br/>Tentativa de acessar /cashflow/v1/** retorna 403
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

No **`services/gateway/ocelot.json`** atual, as rotas **não** definem `AddHeadersToRequest` nem `DownstreamHeaderTransform`. O **Ocelot** atua como **proxy HTTP**: o cliente envia `Authorization: Bearer <JWT>` e esse header (junto com os demais da requisição de origem) costuma **chegar às APIs downstream** sem ser substituído pelo gateway. A identidade do usuário continua disponível no **JWT** que a **CashFlow / Dashboard API revalida** com `AddJwtBearer`.

Se no futuro for necessário expor `sub` ou roles como headers separados (por exemplo para logging ou integrações legadas), usa-se a documentação do Ocelot sobre [**header transformation**](https://ocelot.readthedocs.io/en/latest/features/headerstransformation.html), por exemplo:

```json
"AddHeadersToRequest": {
  "X-User-Id": "Claims[sub] > value"
}
```

> **Importante:** qualquer extensão desse tipo deve ser adicionada **explicitamente** ao `ocelot.json` e revisada no PR — **não** está habilitada no repositório hoje.

---

## Acesso às ferramentas de operação — VPN obrigatória

As ferramentas de observabilidade e administração (**Grafana, Kibana, Prometheus e RabbitMQ Management**) **não possuem rota no Ingress** e não são acessíveis pela internet em nenhum ambiente.

O acesso operacional exige **conexão prévia à VPN** da infraestrutura. A VPN é provisionada fora deste repositório — na camada de rede do cloud provider ou servidor dedicado — e é o mecanismo de isolamento de rede primário para o plano de controle.

```
Operador / Dev
  └─ conecta VPN (WireGuard / Tailscale / OpenVPN)
       └─ obtém IP no range <vpn-cidr>
            └─ acessa IP interno do cluster
                 ├─ kubectl port-forward <pod> <porta>:<porta>
                 └─ ou Service ClusterIP (sem Ingress público)
```

```mermaid
sequenceDiagram
    actor OPS as Operador / Dev
    participant VPN as VPN Gateway
    participant NP as NetworkPolicy K8s
    participant OBS as Grafana / Kibana / Prometheus / RabbitMQ Mgmt

    OPS->>VPN: autentica e conecta túnel VPN
    VPN-->>OPS: IP atribuído no range vpn-cidr

    OPS->>NP: requisição originada de vpn-cidr
    NP->>NP: valida ipBlock.cidr
    alt IP dentro do CIDR da VPN
        NP->>OBS: libera acesso
        OBS-->>OPS: 200 OK — interface disponível
    else IP fora do CIDR
        NP-->>OPS: conexão bloqueada
    end
```

### Reforço via NetworkPolicy (Kubernetes)

O isolamento da VPN é reforçado em nível de rede do cluster pelo manifesto `infra/k8s/base/networkpolicy-observability.yaml`. Cada ferramenta tem uma `NetworkPolicy` dedicada que:

- Define `policyTypes: [Ingress]` — nenhum ingress é permitido por padrão
- Libera ingress **exclusivamente do CIDR da VPN** (`ipBlock.cidr: <vpn-cidr>`)
- Mantém o RabbitMQ AMQP (:5672) acessível pelos pods de negócio internamente, isolando apenas a Management UI (:15672) para o CIDR da VPN

> **Ajuste obrigatório antes do deploy:** substitua `<vpn-cidr>` pelo CIDR real da sua VPN no arquivo `networkpolicy-observability.yaml`.
> - WireGuard típico: `10.8.0.0/24`
> - Tailscale: `100.64.0.0/10`

> **Pré-requisito K8s:** o CNI do cluster deve suportar `NetworkPolicy` — Calico ou Cilium. Já documentado em `docs/operations/kubernetes.md`.

---

## Referências

- [OAuth 2.0 — Client Credentials Grant (RFC 6749)](https://tools.ietf.org/html/rfc6749#section-4.4)
- [Keycloak — Service Accounts](https://www.keycloak.org/docs/latest/server_admin/#_service_accounts)
- [Ocelot — Headers Transformation](https://ocelot.readthedocs.io/en/latest/features/headerstransformation.html)
- [RabbitMQ — Access Control](https://www.rabbitmq.com/access-control.html)
- [OWASP — Microservices Security Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Microservices_Security_Cheat_Sheet.html)
