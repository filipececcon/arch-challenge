# Manual de deploy — ambiente local e produção

Este guia descreve, passo a passo, como subir o sistema no **ambiente de desenvolvimento local** (Docker Compose e/ou executáveis) e no **ambiente produtivo** (Kubernetes com Kustomize).

---

## Parte 1 — Ambiente local

### 1.1 Pré-requisitos

| Ferramenta | Uso |
|------------|-----|
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Infraestrutura, APIs e stack de observabilidade em contêiner |
| [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | Gateway e backends, se rodar fora do Docker |
| [Node.js 20+](https://nodejs.org/) e [Angular CLI](https://angular.dev/tools/cli) | Frontend em `services/frontend` |

**Docker — memória:** o [Elasticsearch](https://www.elastic.co/guide/en/elasticsearch/reference/current/docker.html) no Compose usa heap JVM (~512 MB) e precisa de folga para o SO. Recomenda-se **pelo menos ~4 GB de RAM atribuídos ao Docker Desktop** (ajuste em *Settings → Resources*). Na primeira subida, **Elasticsearch** e **Kibana** podem levar **1–3 minutos** até ficarem *healthy*.

---

### 1.2 O que o `docker-compose.yml` sobe

Na raiz do repositório, um único `docker compose up` sobe **todos** os serviços abaixo (rede `cashflow-network`).

| Grupo | Serviço Compose | Função |
|-------|-----------------|--------|
| Dados / mensagens | `postgres` | PostgreSQL (bases `cashflow_db`, `dashboard_db`, `keycloak_db`, etc.) |
| | `rabbitmq` | RabbitMQ (AMQP + Management UI) |
| Identidade | `keycloak` | Keycloak (realm `cashflow` importado de `infra/keycloak/`) |
| Observabilidade | `elasticsearch` | Armazenamento de logs/dados para Elastic Stack |
| | `kibana` | UI (Discover, APM, etc.) |
| | `apm-server` | Ingestão de traces (Elastic APM) |
| | `prometheus` | Scrape de `/metrics` das APIs |
| | `grafana` | Dashboards (datasource Prometheus provisionado) |
| Aplicação | `cashflow-api` | API CashFlow (ASP.NET Core) |
| | `dashboard-api` | API Dashboard (ASP.NET Core) |
| | `gateway` | API Gateway Ocelot |

**Não está no Compose:** **Fluent Bit** (ingestão de logs a partir do stdout dos containers) permanece planeado conforme [ADR-011](../decisions/ADR-011-fluent-bit-ingestor-de-logs.md) e notas em [`observability.md`](./observability.md).

**Volumes persistentes:** `postgres_data`, `rabbitmq_data`, `elasticsearch_data`, `prometheus_data`, `grafana_data`. Um `docker compose down -v` **apaga** também estes dados (incluindo índices e séries temporais locais).

---

### 1.3 Opção A — Stack completa com Docker Compose (recomendado para testar ponta a ponta)

1. **Subir tudo (com build das imagens .NET)**

   ```bash
   cd /caminho/para/arch-challange
   docker compose up -d --build
   ```

   Na primeira execução o build e a inicialização do Elastic Stack podem levar vários minutos.

2. **Conferir saúde dos serviços**

   - `postgres` e `rabbitmq` têm healthcheck; `elasticsearch` e `kibana` também (Kibana depende do ES *healthy*).
   - APIs e gateway sobem após dependências mínimas (Postgres/RabbitMQ/Keycloak conforme definido no Compose).

3. **URLs na sua máquina (host)**

   | Serviço | URL |
   |---------|-----|
   | API Gateway (entrada unificada) | http://localhost:5000 |
   | Swagger unificado | http://localhost:5000/swagger |
   | CashFlow API (direto) | http://localhost:5001 |
   | Dashboard API (direto) | http://localhost:5002 |
   | Keycloak | http://localhost:8080 |
   | RabbitMQ Management | http://localhost:15672 |
   | Elasticsearch | http://localhost:9200 |
   | Kibana | http://localhost:5601 |
   | Elastic APM Server | http://localhost:8200 |
   | Prometheus | http://localhost:9090 |
   | Grafana | http://localhost:3000 |

   **Credenciais** (apenas desenvolvimento local), nomes DNS internos e ficheiros de configuração (Prometheus, Grafana, APM): ver secção *Docker Compose — observabilidade* e *Acessos locais* em **[`observability.md`](./observability.md)**.

4. **Frontend (fora do Compose)**

   O SPA Angular não está no `docker-compose`; noutro terminal:

   ```bash
   cd services/frontend
   npm install
   npm run dev
   ```

   Por defeito o Angular serve em **http://localhost:4200** (alinhar redirect URIs no Keycloak se alterar a porta).

5. **Realm e utilizadores de teste**

   O realm `cashflow` é importado a partir de [`infra/keycloak/cashflow-realm.json`](../../infra/keycloak/cashflow-realm.json). Utilizadores e roles: [README principal](../../README.md) (secção Keycloak).

6. **Reset completo do ambiente local**

   ```bash
   docker compose down -v
   docker compose up -d --build
   ```

   Isto recria volumes (Postgres, RabbitMQ, Elasticsearch, Prometheus, Grafana, etc.).

---

### 1.4 Opção B — Infraestrutura (e opcionalmente observabilidade) no Docker + APIs na máquina

Útil para depurar o código .NET com hot reload, sem reconstruir imagens das APIs.

1. **Subir apenas o necessário para as APIs**

   Mínimo para CashFlow, Dashboard e Gateway em modo `dotnet run`:

   ```bash
   docker compose up -d postgres rabbitmq keycloak
   ```

   Se também precisar de **Kibana, APM, Prometheus ou Grafana** à medida que desenvolve (métricas/traces na mesma rede que os containers usariam):

   ```bash
   docker compose up -d postgres rabbitmq keycloak elasticsearch kibana apm-server prometheus grafana
   ```

   **Nota:** sem `cashflow-api`, `dashboard-api` e `gateway` no Compose, o Prometheus deixa de ter esses targets como DNS interno a menos que aponte para o host — ajuste [`infra/prometheus/prometheus.yml`](../../infra/prometheus/prometheus.yml) se for testar scrape contra `host.docker.internal` ou similar.

2. **Executar Gateway, CashFlow e Dashboard** (terminais separados)

   ```bash
   # Terminal 1 — Gateway → http://localhost:5000
   cd services/gateway
   dotnet restore && dotnet run
   ```

   ```bash
   # Terminal 2 — CashFlow → http://localhost:5001
   cd services/cashflow
   dotnet restore && dotnet run --project src/Api/Api.csproj
   ```

   ```bash
   # Terminal 3 — Dashboard → http://localhost:5002
   cd services/dashboard
   dotnet restore && dotnet run --project src/Api/Api.csproj
   ```

3. **Frontend**

   ```bash
   cd services/frontend
   npm install && npm run dev
   ```

4. **Configuração**

   Use os `appsettings.Development.json` de cada API e o Ocelot em [`services/gateway/ocelot.Development.json`](../../services/gateway/ocelot.Development.json) (downstreams `localhost:5001` e `localhost:5002`).

---

### 1.5 Testes automatizados (local)

```bash
cd services/cashflow && dotnet test
cd services/dashboard && dotnet test
```

---

## Parte 2 — Ambiente produtivo (Kubernetes)

A estratégia detalhada está em [`kubernetes.md`](./kubernetes.md); abaixo está o roteiro resumido.

O **Compose local** inclui Elasticsearch, Kibana, APM, Prometheus e Grafana; o **overlay de Kubernetes** descrito no repositório foca aplicação e dependências acordadas nos manifestos (não replica necessariamente toda a stack de observabilidade do Compose). Para produção, costuma-se usar stacks gerenciadas ou pipelines dedicados para métricas/logs.

### 2.1 Pré-requisitos no cluster

- **Ingress controller** (ex.: ingress-nginx), se for expor HTTP(S).
- **metrics-server**, se usar o **HorizontalPodAutoscaler** do CashFlow.
- CNI com **NetworkPolicy** (se for aplicar a política em `infra/k8s/base`).
- **cert-manager** (opcional), para TLS no Ingress.

### 2.2 Build e publicação das imagens

Os Dockerfiles **CashFlow** e **Dashboard** usam **contexto na raiz do repositório**. O **Gateway** usa o diretório `services/gateway`.

**Imagens esperadas pelo overlay de produção atual** ([`infra/k8s/overlays/production/kustomization.yaml`](../../infra/k8s/overlays/production/kustomization.yaml)): apenas **CashFlow API** e **Gateway**.

```bash
docker build -f services/cashflow/Dockerfile -t SEU_REGISTRY/arch-challenge-cashflow-api:1.0.0 .
docker build -f services/gateway/Dockerfile -t SEU_REGISTRY/arch-challenge-gateway:1.0.0 ./services/gateway
```

O **Dashboard API** também tem Dockerfile e entra no **Compose local**; ainda **não** há Deployment correspondente no overlay documentado. Para gerar a imagem (Compose ou futuro manifesto):

```bash
docker build -f services/dashboard/Dockerfile -t SEU_REGISTRY/arch-challenge-dashboard-api:1.0.0 .
```

Envie as imagens necessárias para o registry que o cluster (ou o pipeline) acessa (`docker push` ou CI).

### 2.3 Segredos e overlay de produção

1. Copie o exemplo de credenciais:

   ```bash
   cp infra/k8s/overlays/production/credentials.env.example infra/k8s/overlays/production/credentials.env
   ```

2. Edite `credentials.env` com senhas fortes. A connection string do CashFlow deve usar o **mesmo** `postgres-password` definido para o Postgres (ver comentários no ficheiro).

3. Edite [`infra/k8s/overlays/production/kustomization.yaml`](../../infra/k8s/overlays/production/kustomization.yaml): ajuste `images:` (`newName` / `newTag`) para o seu registry e tags reais.

### 2.4 Aplicar o manifesto

Na raiz do repositório:

```bash
kubectl apply -k infra/k8s/overlays/production
```

Isto cria o namespace `arch-challenge`, dependências (Postgres, RabbitMQ, Keycloak), workloads definidos no overlay, Ingress, PDB, HPA e NetworkPolicy, conforme os manifestos.

### 2.5 Pós-deploy obrigatório

1. **Migrações EF Core:** em produção as APIs normalmente **não** aplicam migrações automaticamente no startup. Execute migrações no CI/CD ou com um Job controlado antes ou junto ao rollout.
2. **DNS e TLS:** ajuste o host em [`infra/k8s/base/ingress.yaml`](../../infra/k8s/base/ingress.yaml) (ex.: `api.arch-challenge.example`) e TLS/anotações conforme o Ingress controller.
3. **Keycloak e JWT:** se clientes externos (browser, APIs) falharem na validação do token, alinhe a URL pública do Keycloak (`KEYCLOAK_AUTHORITY`) com a usada nos clients do realm — ver notas em [`kubernetes.md`](./kubernetes.md).

### 2.6 Produção “real”

Para cargas reais, prefira **serviços gerenciados** (PostgreSQL, fila, IdP, observabilidade) em vez de operar tudo no cluster, mantendo os mesmos contratos de conexão. O overlay atual inclui dependências em cluster como **referência** para ambientes auto-hospedados.

### 2.7 Escopo atual do manifesto de K8s

Confira [`infra/k8s/README.md`](../../infra/k8s/README.md) e [`kubernetes.md`](./kubernetes.md): o overlay inclui **CashFlow API** e **Gateway** (e dependências). **Dashboard API** e **frontend** ainda não estão descritos como Deployments no repositório; quando forem adicionados, siga o mesmo padrão (imagens, Service, variáveis, roteamento no Gateway/Ingress).

---

## Referências rápidas

| Documento | Conteúdo |
|-----------|----------|
| [`README.md`](../../README.md) | Visão geral, utilizadores Keycloak de teste |
| [`observability.md`](./observability.md) | URLs, credenciais e ficheiros da stack local de observabilidade |
| [`infra/k8s/README.md`](../../infra/k8s/README.md) | Layout Kustomize |
| [`kubernetes.md`](./kubernetes.md) | Deploy K8s em detalhe |
| [`docs/security/authorization.md`](../security/authorization.md) | Papéis e autorização |
