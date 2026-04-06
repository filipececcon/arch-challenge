# Arquitetura do Sistema — Diagramas C4

O sistema é documentado seguindo o [modelo C4](https://c4model.com/), que descreve a arquitetura em quatro níveis progressivos de detalhamento.

> **Fonte:** Os diagramas foram gerados com [draw.io](https://www.drawio.com/) — o arquivo fonte editável está em [`diagrams/Architecture.drawio`](./diagrams/Architecture.drawio).

---

## Nível 1 — Context (C1)

Visão de mais alto nível: mostra o sistema como uma caixa preta e seus relacionamentos com usuários e sistemas externos.

![Diagrama C1 — Context](./diagrams/Architecture-C1%20-%20Context.png)

**O que este diagrama mostra:**
- Os atores que interagem com o sistema (Comerciante e Gestor)
- O sistema de Controle de Fluxo de Caixa como unidade
- Dependências externas: Keycloak (autenticação)

---

## Nível 2 — Container (C2)

Detalha os containers que compõem o sistema: aplicações, bancos de dados, mensageria e serviços de suporte.

![Diagrama C2 — Container](./diagrams/Architecture-C2%20-%20Container.png)

**O que este diagrama mostra:**
- **API Gateway (Ocelot)** — ponto único de entrada, autenticação JWT e roteamento
- **CashFlow Backend** — API ASP.NET Core para registro de lançamentos
- **Dashboard Backend** — API ASP.NET Core para consolidado diário
- **CashFlow Frontend** e **Dashboard Frontend** — SPAs Angular
- **PostgreSQL** — banco de dados dedicado por serviço (Database per Service)
- **RabbitMQ** — broker de mensagens para comunicação assíncrona entre serviços
- **Keycloak** — Identity Provider (OAuth 2.0 / OIDC)

---

## Nível 3 — Component (C3)

Aprofunda a visão interna de cada container, mostrando os componentes e suas responsabilidades.

![Diagrama C3 — Components](./diagrams/Architecture-C3%20-%20Components.png)

**O que este diagrama mostra:**
- Camadas internas das APIs (Controllers, Services, Repositories, Domain)
- Publicação e consumo de eventos via RabbitMQ (`LancamentoRegistrado`)
- Integração do Gateway com o Keycloak para validação de JWT
- Separação clara entre os bounded contexts CashFlow e Dashboard

---

## Nível 4 — Code (C4)

Diagrama de classes gerado diretamente pela IDE, detalhando a estrutura de código de cada componente.

> Em elaboração — será adicionado por serviço à medida que a implementação avançar.

---

## Decisões arquiteturais relacionadas

| ADR | Decisão |
|---|---|
| [ADR-002](../decisions/ADR-002-separacao-cashflow-dashboard.md) | Separação em dois bounded contexts: CashFlow e Dashboard |
| [ADR-003](../decisions/ADR-003-comunicacao-assincrona-rabbitmq.md) | Comunicação assíncrona via RabbitMQ |
| [ADR-004](../decisions/ADR-004-backend-aspnet-core.md) | Backend com ASP.NET Core |
| [ADR-008](../decisions/ADR-008-autenticacao-autorizacao-keycloak.md) | Autenticação e autorização com Keycloak |
| [ADR-009](../decisions/ADR-009-api-gateway-ocelot.md) | API Gateway com Ocelot |
