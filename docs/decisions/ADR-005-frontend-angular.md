# ADR-005 — Frontend com Angular

- **Status:** Aceito
- **Data:** 2026-04-03
- **Decisores:** Time de Arquitetura

---

## Contexto

Os domínios CashFlow e Dashboard precisavam de uma interface de usuário. Era necessário escolher o framework de frontend e a abordagem de estilização para a SPA unificada (ver ADR-010).

A empresa solicitante do desafio já utiliza Angular como parte de sua stack frontend, tornando a escolha estratégica — além de facilitar manutenção e onboarding, garante consistência com os padrões de desenvolvimento já estabelecidos na organização.

---

## Decisão

Utilizar **Angular** (TypeScript) como framework de frontend, em um **único projeto unificado** que abrange os domínios CashFlow e Dashboard como feature modules com lazy loading (ver ADR-010).

### Versão e padrões adotados

- Angular 19 (com Standalone Components e esbuild)
- Tailwind CSS como biblioteca de estilos utilitários
- Angular Router para navegação SPA e lazy loading de feature modules
- HttpClient com interceptors para injeção automática de tokens JWT
- RxJS para gerenciamento de estado reativo
- Implementação manual de OIDC/PKCE (sem biblioteca de terceiros) para integração com Keycloak

### Organização interna do projeto

```
services/frontend/
├── src/
│   ├── app/
│   │   ├── core/               ← Serviços singleton, guards, interceptors, auth
│   │   └── features/
│   │       ├── cashflow/       ← Feature module lazy-loaded (lançamentos)
│   │       │   ├── pages/
│   │       │   ├── components/
│   │       │   └── services/
│   │       └── dashboard/      ← Feature module lazy-loaded (consolidado)
│   │           ├── pages/
│   │           ├── components/
│   │           └── services/
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
- Tailwind CSS permite estilização rápida e consistente sem overhead de uma biblioteca de componentes
- Implementação manual de OIDC/PKCE oferece controle total sobre o fluxo de autenticação sem dependências externas

**Negativas:**
- Maior verbosidade em relação a React ou Vue para componentes simples
- Tempo de build mitigado pelo Standalone Components e esbuild nativo no Angular 19
- Bundle size inicial maior que React, mas mitigado por lazy loading por módulo/feature

---

## Referências

- [Angular Documentation](https://angular.dev/)
- [Tailwind CSS](https://tailwindcss.com/)
- [OAuth 2.0 PKCE — RFC 7636](https://tools.ietf.org/html/rfc7636)
