# ADR-005 — Frontend com Angular

- **Status:** Aceito
- **Data:** 2026-04-03
- **Decisores:** Time de Arquitetura

---

## Contexto

Os serviços CashFlow e Dashboard possuem interfaces de usuário distintas. Era necessário escolher o framework de frontend para construção dessas interfaces.

A empresa solicitante do desafio já utiliza Angular como parte de sua stack frontend, tornando a escolha estratégica — além de facilitar manutenção e onboarding, garante consistência com os padrões de desenvolvimento já estabelecidos na organização.

---

## Decisão

Utilizar **Angular** (TypeScript) como framework de frontend, em um **único projeto unificado** que abrange os domínios CashFlow e Dashboard como feature modules com lazy loading (ver ADR-010).

### Versão e padrões adotados

- Angular 17+ (com Standalone Components)
- Angular Material como biblioteca de componentes UI
- Angular Router para navegação SPA e lazy loading de feature modules
- HttpClient com interceptors para injeção automática de tokens JWT
- RxJS para gerenciamento de estado reativo
- OIDC Client (angular-auth-oidc-client) para integração com Keycloak

### Organização interna do projeto

```
services/frontend/
├── src/
│   ├── app/
│   │   ├── core/               ← Serviços singleton, guards, interceptors, auth
│   │   ├── shared/             ← Componentes, pipes e diretivas reutilizáveis
│   │   ├── cashflow/           ← Feature module lazy-loaded (lançamentos)
│   │   │   ├── pages/
│   │   │   ├── components/
│   │   │   └── services/
│   │   └── dashboard/          ← Feature module lazy-loaded (consolidado)
│   │       ├── pages/
│   │       ├── components/
│   │       └── services/
│   ├── environments/
│   └── assets/
└── angular.json
```

---

## Alternativas Consideradas

### React / Next.js

**Prós:**
- Ecossistema muito grande, grande disponibilidade de libs
- Flexibilidade na organização do projeto

**Contras:**
- Fora da stack da empresa solicitante
- Menos opinativo, o que pode gerar inconsistência de padrões entre times
- SSR com Next.js adiciona complexidade de infraestrutura desnecessária para SPAs internas

**Descartado** por não estar alinhado com a stack da empresa.

### Vue / Nuxt

**Prós:**
- Curva de aprendizado menor
- Boa DX (Developer Experience)

**Contras:**
- Fora da stack da empresa solicitante
- Menor adoção em contextos enterprise

**Descartado** por não estar alinhado com a stack da empresa.

---

## Consequências

**Positivas:**
- Alinhamento com a stack da empresa solicitante
- Framework opinativo que impõe estrutura e convenções — facilita manutenção em times maiores
- TypeScript de primeira classe, reduzindo erros em tempo de execução
- Integração nativa e bem documentada com Keycloak via `angular-auth-oidc-client`
- Angular Material oferece componentes prontos de alta qualidade para interfaces enterprise

**Negativas:**
- Maior verbosidade em relação a React ou Vue para componentes simples
- Tempo de build mais lento em projetos grandes (mitigado pelo Standalone Components e esbuild no Angular 17+)
- Bundle size inicial maior que React, mas mitigado por lazy loading por módulo/feature

---

## Referências

- [Angular Documentation](https://angular.dev/)
- [angular-auth-oidc-client](https://github.com/damienbod/angular-auth-oidc-client)
- [Angular Material](https://material.angular.io/)
