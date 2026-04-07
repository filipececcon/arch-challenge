# ADR-001 — Monorepo com CI/CD Independente por Serviço

- **Status:** Aceito
- **Data:** 2026-04-03
- **Decisores:** Time de Arquitetura

---

## Contexto

O sistema é composto por dois serviços distintos: **CashFlow** (controle de lançamentos) e **Dashboard** (consolidado diário). Cada serviço possui seu próprio backend (ASP.NET Core) e frontend (Angular), com ciclos de vida e times de ownership potencialmente independentes.

Era necessário decidir entre manter os serviços em repositórios separados (polyrepo) ou em um único repositório (monorepo).

Um requisito não funcional crítico é que o CashFlow **não pode ficar indisponível** caso o Dashboard falhe, o que exige que os deploys sejam completamente independentes.

---

## Decisão

Adotar **monorepo único** com **pipelines de CI/CD independentes por serviço**, utilizando path filters no GitHub Actions para disparar apenas o pipeline do serviço que sofreu alteração.

---

## Estrutura

```
arch-challenge/
├── services/
│   ├── gateway/                ← API Gateway (Ocelot)
│   ├── cashflow/               ← API ASP.NET Core (CashFlow)
│   ├── dashboard/              ← API ASP.NET Core (Dashboard)
│   └── frontend/               ← SPA Angular unificada (ver ADR-010)
│       └── src/app/
│           ├── core/
│           └── features/
│               ├── cashflow/   ← Feature module lazy-loaded
│               └── dashboard/  ← Feature module lazy-loaded
├── infra/                      ← K8s (Kustomize), init Postgres, realm Keycloak
│   ├── k8s/
│   ├── postgres/
│   └── keycloak/
├── docs/
│   ├── architecture/
│   ├── security/
│   ├── decisions/
│   └── operations/
├── docker-compose.yml
└── README.md
```

Cada serviço/camada possui seu próprio workflow de CI/CD:

```yaml
# .github/workflows/cashflow-backend.yml
on:
  push:
    paths:
      - 'services/cashflow/**'

# .github/workflows/frontend.yml
on:
  push:
    paths:
      - 'services/frontend/**'
```

---

## Alternativas Consideradas

### Polyrepo (dois repositórios separados)

**Prós:**
- Ownership e permissões totalmente isolados por repositório
- Pipelines completamente independentes sem necessidade de path filters

**Contras:**
- Dificuldade em compartilhar contratos de eventos, tipos e bibliotecas comuns
- Versionamento de contrato entre serviços se torna complexo e propenso a erros
- Overhead de configuração duplicada (lint, security scan, base de imagens Docker)
- Avaliadores e stakeholders precisam navegar em múltiplos repositórios para entender o sistema como um todo

---

## Consequências

**Positivas:**
- Repositório público único facilita a avaliação do desafio
- Contratos de mensagens documentados nos ADRs e mantidos alinhados entre produtor (CashFlow) e consumidor (Dashboard) no código de cada serviço
- ADRs, diagramas e documentação centralizados
- Deploy independente garantido via path filters no CI/CD

**Negativas:**
- Requer disciplina para não criar acoplamento entre os serviços pelo código compartilhado
- Pipelines precisam de configuração cuidadosa para evitar builds desnecessários

---

## Referências

- [Monorepo vs Polyrepo — ThoughtWorks Technology Radar](https://www.thoughtworks.com/radar)
- [GitHub Actions — Path Filtering](https://docs.github.com/en/actions/writing-workflows/workflow-syntax-for-github-actions#onpushpull_requestpull_request_targetpathspaths-ignore)
