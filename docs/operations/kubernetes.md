# Deploy em Kubernetes (produção)

Este documento descreve a **estratégia de deploy** no Kubernetes, alinhada ao requisito de operação do desafio (deploy, escalabilidade, recuperação de falhas) e aos demais artefatos em [`observability.md`](./observability.md) e [`../security`](../security/README.md).

A pasta [`infra/`](../../infra/README.md) documenta a organização completa da infraestrutura no repositório (K8s, Postgres, Keycloak e relação com o Docker Compose).

## Visão geral

| Camada | Manifestos | Observação |
|--------|------------|------------|
| Aplicações | `infra/k8s/base/` — CashFlow API, API Gateway | Imagens publicadas no seu registry |
| Dependências em cluster | `infra/k8s/dependencies/` — PostgreSQL, RabbitMQ, Keycloak | StatefulSets com **PVC** para dados persistentes (Postgres + RabbitMQ) |
| Produção | `infra/k8s/overlays/production/` — Kustomize, segredos, tags de imagem | Ajuste hosts, TLS, registry e opcionalmente `storageClassName` |

**Recomendação para produção real:** preferir **serviços gerenciados** (PostgreSQL, fila, IdP) em vez de operar StatefulSets de banco e Keycloak no próprio cluster, mantendo os mesmos contratos de conexão (variáveis e connection strings).

## Armazenamento persistente (PVC)

Os manifests **não** declaram `PersistentVolume` estáticos: o cluster deve providenciar volumes através de um **StorageClass** (provisioning dinâmico).

| Workload | Recurso | PVC | Tamanho pedido |
|----------|---------|-----|----------------|
| PostgreSQL | `StatefulSet` `postgres` | `volumeClaimTemplates` → `data-postgres-0` | 10 Gi |
| RabbitMQ | `StatefulSet` `rabbitmq` | `volumeClaimTemplates` → `data-rabbitmq-0` | 5 Gi |

- Se existir **StorageClass default**, os PVCs ficam `Bound` sem alterar os YAML.
- Se os PVCs ficarem **Pending**, defina `storageClassName` no `spec.volumeClaimTemplates[].spec` de cada `StatefulSet` (ou crie um StorageClass default no cluster). Em clouds comuns as classes chamam-se por exemplo `gp3`, `standard`, `premium-rwo` — consulte `kubectl get storageclass`.

O serviço **RabbitMQ** é **headless** (`clusterIP: None`), adequado ao `StatefulSet` e compatível com o hostname `rabbitmq` já usado no `ConfigMap` (`RABBITMQ_HOST`).

## Pré-requisitos do cluster

- **Ingress controller** (ex.: ingress-nginx) se for expor HTTP(S) externamente.
- **metrics-server** instalado se for usar o **HorizontalPodAutoscaler** do CashFlow API.
- CNI com suporte a **NetworkPolicy** se for aplicar a política incluída em `base` (Calico, Cilium, etc.).
- **Cert-manager** (opcional) para TLS no Ingress — ver anotações comentadas em `ingress.yaml`.

## Build e publicação de imagens

Os Dockerfiles esperam contexto na **raiz do repositório** quando o build precisa de caminhos relativos ao monorepo (por exemplo, CashFlow e Dashboard):

```bash
docker build -f services/cashflow/Dockerfile -t ghcr.io/SEU_ORG/arch-challenge-cashflow-api:1.0.0 .
docker build -f services/gateway/Dockerfile -t ghcr.io/SEU_ORG/arch-challenge-gateway:1.0.0 .
```

Atualize `newName` / `newTag` em `infra/k8s/overlays/production/kustomization.yaml` para apontar para o seu registry.

## Segredos (obrigatório antes do apply)

1. Copie `infra/k8s/overlays/production/credentials.env.example` para `credentials.env` (arquivo **fora do Git** — já listado no `.gitignore`).
2. Preencha senhas fortes e a connection string do CashFlow com o **mesmo** `postgres-password` usado pelo Postgres.

O Kustomize gera o Secret `arch-challenge-credentials` a partir desse arquivo.

## Aplicar o overlay de produção

```bash
kubectl apply -k infra/k8s/overlays/production
```

Isso cria o namespace `arch-challenge`, dependências, workloads, Ingress, PDB, HPA e NetworkPolicy.

## Pós-deploy

1. **Migrações EF Core:** em `Production`, a API não executa migrações automáticas no startup (comportamento típico). Execute migrações no pipeline (CI) ou com um Job controlado antes ou junto ao rollout.
2. **Keycloak e DNS:** o `ConfigMap` `arch-challenge-config` usa o authority interno do cluster. Se o browser ou clientes externos falharem na validação JWT, ajuste `KEYCLOAK_AUTHORITY` para a URL **pública** do Keycloak (via Ingress) e alinhe clients e redirect URIs no realm.
3. **Ingress:** edite `infra/k8s/base/ingress.yaml` (host `api.arch-challenge.example`) ou sobrescreva com um patch no overlay.

## Escalabilidade e recuperação

| Recurso | Função |
|---------|--------|
| **HPA** (`cashflow-hpa.yaml`) | Escala o CashFlow API por CPU (ajuste `minReplicas`/`maxReplicas` conforme carga). |
| **PDB** | Garante pelo menos um pod disponível durante interrupções voluntárias (drain). |
| **Probes** | `tcpSocket` nas portas HTTP 8080 — substitua por `/health` quando existir endpoint de readiness/liveness. |
| **Rolling updates** | Padrão do Deployment (`revisionHistoryLimit` definido). |

## Observabilidade

As aplicações seguem o modelo descrito em [observability.md](./observability.md): logs em stdout (JSON), métricas Prometheus em `/metrics` quando habilitadas no código, traces via Elastic APM conforme configuração.

Em Kubernetes, o coletor de logs costuma ser um **DaemonSet** (ex.: Fluent Bit) com pipeline para Elasticsearch; métricas podem ser coletadas via **ServiceMonitor** (Prometheus Operator) ou scrape estático — anotações `prometheus.io/*` podem ser adicionadas aos Deployments quando adotar esse padrão.

## Manutenção dos artefatos em `dependencies/files/`

- `init.sql` deve permanecer alinhado a [`infra/postgres/init.sql`](../../infra/postgres/init.sql).
- `cashflow-realm.json` deve permanecer alinhado a [`infra/keycloak/cashflow-realm.json`](../../infra/keycloak/cashflow-realm.json).

O Kustomize só permite referências a arquivos **dentro** da árvore do `kustomization` dos dependencies; por isso existe cópia sob `infra/k8s/dependencies/files/`.

## Dashboard API e frontend

Quando existirem imagens e configuração para o Dashboard e o frontend, siga o mesmo padrão: Deployment + Service + variáveis (connection string, RabbitMQ, Keycloak) e roteamento no Gateway/Ingress — espelhando o que já está documentado para os serviços atuais.
