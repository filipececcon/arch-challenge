# ADR-010 — Frontend Unificado com Feature Modules Lazy-Loaded

- **Status:** Aceito
- **Data:** 2026-04-05
- **Decisores:** Time de Arquitetura

---

## Contexto

Os domínios CashFlow (lançamentos) e Dashboard (consolidado diário) possuem interfaces de usuário distintas, mas compartilham infraestrutura transversal: autenticação via Keycloak, interceptors de token JWT, componentes visuais comuns e configurações de ambiente.

Era necessário decidir se o frontend seria desenvolvido como:

1. **Dois projetos Angular separados** — um por bounded context (cashflow e dashboard)
2. **Um único projeto Angular** com os domínios organizados como feature modules isolados
3. **Micro Frontends (MFE)** com Module Federation — cada domínio como uma aplicação Angular independente

---

## Decisão

Adotar **um único projeto Angular** com os domínios CashFlow e Dashboard organizados como **feature modules com lazy loading**, dentro de uma estrutura modular por bounded context.

### Organização interna do projeto

```
services/
└── frontend/
    ├── src/
    │   └── app/
    │       ├── core/                  ← Singleton: auth, guards, interceptors, http
    │       ├── shared/                ← Componentes, pipes e diretivas reutilizáveis
    │       ├── cashflow/              ← Feature module — Bounded Context: Lançamentos
    │       │   ├── pages/
    │       │   ├── components/
    │       │   └── services/
    │       └── dashboard/             ← Feature module — Bounded Context: Consolidado
    │           ├── pages/
    │           ├── components/
    │           └── services/
    ├── environments/
    └── angular.json
```

### Padrões adotados

- Cada feature module carregado via **lazy loading** (`loadChildren`) — isolamento de carregamento sem MFE
- Serviços de domínio providos com `providedIn: 'any'` para escopo por feature
- `core/` contém os serviços globais, configurados uma única vez no bootstrap
- `shared/` expõe apenas componentes verdadeiramente reutilizáveis entre os dois domínios

---

## Alternativas Consideradas

### Dois projetos Angular separados (por serviço)

**Prós:**
- Alinhamento físico com a separação de bounded contexts do backend
- Deploy de frontend independente por domínio

**Contras:**
- Duplicação de código transversal: auth, interceptors, componentes base, configuração de ambiente
- Dois processos de build, dois Dockerfiles, dois servidores de hospedagem
- Experiência de usuário fragmentada: o comerciante acessaria duas URLs distintas sem continuidade de sessão
- Compartilhar componentes exigiria publicar uma biblioteca npm interna — complexidade desproporcional ao escopo

**Descartado** por gerar overhead operacional e de desenvolvimento sem benefício justificável para o contexto.

---

### Micro Frontends (MFE) com Module Federation

**Prós:**
- Deploy independente por domínio de frontend
- Times completamente autônomos com bases de código isoladas
- Falha em um MFE não afeta o carregamento do outro

**Contras:**
- Complexidade operacional alta: versionamento de contratos entre MFEs, compatibilidade de dependências compartilhadas (Angular, RxJS, Material)
- Exige uma app shell para orquestrar roteamento e bootstrapping dos remotes
- Autenticação compartilhada entre MFEs requer solução adicional (ex: shared store via Custom Events ou BroadcastChannel)
- Difícil de justificar com dois domínios e um único time — o custo de manutenção excede o ganho de autonomia
- Ferramental ainda em maturação no ecossistema Angular (Module Federation via `@angular-architects/module-federation`)

**Descartado** por ser over-engineering para o escopo atual. A separação de domínios no backend via serviços independentes já satisfaz o requisito de autonomia arquitetural. O frontend é a camada de apresentação — não precisa replicar a mesma granularidade de separação física.

---

## Consequências

**Positivas:**
- Um único projeto para desenvolver, testar e fazer deploy
- Código transversal (auth, guards, interceptors, componentes base) compartilhado sem overhead
- Lazy loading garante que o bundle do Dashboard só é carregado quando o usuário navega para ele
- Experiência de usuário unificada: uma única SPA, mesma sessão de autenticação
- CI/CD simplificado: um único pipeline de build e deploy de frontend

**Negativas:**
- Um único build quebrado afeta os dois domínios no frontend (mitigado por testes automatizados e pipelines com lint + type-check)
- Times de frontend não conseguem fazer deploy de um domínio sem impactar o outro (trade-off aceito)

---

## Referências

- [Angular — Lazy Loading Feature Modules](https://angular.dev/guide/ngmodules/lazy-loading)
- [Micro Frontends — Martin Fowler](https://martinfowler.com/articles/micro-frontends.html)
- [Module Federation com Angular — @angular-architects/module-federation](https://github.com/angular-architects/module-federation-plugin)
