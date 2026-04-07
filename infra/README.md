# Infraestrutura (`infra/`)

Esta pasta concentra **configuração de plataforma** usada no desenvolvimento local e como referência para ambientes Kubernetes: banco, IdP, manifests e cópias alinhadas ao que o Compose monta na raiz do repositório.

---

## Estrutura

```
infra/
├── k8s/                    ← Kubernetes (Kustomize) — workloads e dependências em cluster
│   ├── base/               ← Namespace, CashFlow API, Gateway, Ingress, PDB, HPA, NetworkPolicy
│   ├── dependencies/       ← PostgreSQL, RabbitMQ, Keycloak (+ ConfigMaps com init DB e realm)
│   └── overlays/production/← Imagens, réplicas, Secret a partir de credentials.env
├── apm-server/             ← `apm-server.yml` montado pelo Docker Compose (Elastic APM Server)
├── grafana/                ← Provisioning (ex.: datasource Prometheus) para Grafana no Compose
├── prometheus/             ← `prometheus.yml` — scrape dos serviços no Compose
├── postgres/               ← Script de inicialização de bancos (Docker Compose e espelho lógico para K8s)
└── keycloak/               ← Export do realm `cashflow` (clients, roles, usuários de dev)
```

---

## Capacidades

| Área | O que entrega |
|------|----------------|
| **`k8s/`** | Deploy declarativo com **Kustomize**: aplicações (CashFlow, Gateway), roteamento (Ingress), resiliência (PDB, HPA), rede (NetworkPolicy), ConfigMaps e overlay de **produção** com imagens de registry e segredos gerados a partir de `credentials.env`. Inclui **dependências opcionais** em cluster (Postgres, RabbitMQ, Keycloak) para ambientes auto-hospedados. |
| **`postgres/`** | `init.sql` cria bases e usuários esperados pelos serviços (**database per service**). Usado pelo **Docker Compose** na primeira subida do volume; o mesmo conteúdo é referenciado em `k8s/dependencies/files/` para o cluster (manter os dois alinhados). |
| **`keycloak/`** | `cashflow-realm.json` define realm, clients OIDC, mappers e usuários de teste para **desenvolvimento**. Montado no Keycloak do Compose; cópia em `k8s/dependencies/files/` para import no cluster. |

---

## Onde aprofundar

| Documento | Conteúdo |
|-----------|----------|
| [`k8s/README.md`](./k8s/README.md) | Layout rápido do Kustomize e comando de deploy |
| [`docs/operations/kubernetes.md`](../docs/operations/kubernetes.md) | Estratégia de deploy, build de imagens, segredos e pós-deploy |
| [`docs/operations/manual-deploy.md`](../docs/operations/manual-deploy.md) | Passo a passo local (Compose) e produção (kubectl) |
| [`docs/operations/observability.md`](../docs/operations/observability.md) | Elasticsearch, Kibana, APM Server, Prometheus, Grafana — portas, credenciais e ficheiros em `infra/` |

---

## Manutenção

- Alterações em `postgres/init.sql` ou `keycloak/cashflow-realm.json` devem ser **refletidas** nos ficheiros sob `k8s/dependencies/files/` quando esses forem a fonte usada pelo Kustomize (o Kustomize só referencia ficheiros dentro da árvore do respetivo `kustomization.yaml`). Ver secção *Manutenção dos artefatos* em [`docs/operations/kubernetes.md`](../docs/operations/kubernetes.md).
