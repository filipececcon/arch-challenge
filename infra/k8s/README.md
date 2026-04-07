# Kubernetes (Kustomize)

Visão geral da pasta `infra/` (Postgres, Keycloak, relação com Compose): [`../README.md`](../README.md).

## Layout

| Caminho | Conteúdo |
|---------|----------|
| `base/` | Namespace, CashFlow API, Gateway, Ingress, PDB, HPA, NetworkPolicy |
| `dependencies/` | PostgreSQL e RabbitMQ (**StatefulSet** + **PVC**), Keycloak + ConfigMaps (init DB e realm) |
| `overlays/production/` | Imagens, réplicas do Gateway, Secret gerado a partir de `credentials.env` |

## Deploy rápido

1. Criar `overlays/production/credentials.env` a partir de `credentials.env.example`.
2. Ajustar `overlays/production/kustomization.yaml` (`images:` → seu registry).
3. `kubectl apply -k overlays/production`

Documentação detalhada: [`docs/operations/kubernetes.md`](../../docs/operations/kubernetes.md).
