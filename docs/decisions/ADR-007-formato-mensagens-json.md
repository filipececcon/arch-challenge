# ADR-007 — Formato de Mensagens: JSON sem Schema Registry

- **Status:** Aceito
- **Data:** 2026-04-03
- **Decisores:** Time de Arquitetura

---

## Contexto

Com a adoção do RabbitMQ como broker de mensagens (ADR-003), era necessário definir o formato de serialização das mensagens trocadas entre CashFlow e Dashboard.

As principais opções consideradas foram:

- **JSON** — formato texto, sem necessidade de schema registry
- **Avro** — formato binário, requer Confluent Schema Registry ou equivalente
- **Protobuf** — formato binário do Google, requer geração de código a partir de `.proto`
- **MessagePack** — formato binário compacto, sem schema registry obrigatório

---

## Decisão

Utilizar **JSON** como formato de serialização das mensagens, sem a adoção de schema registry.

O contrato dos eventos será documentado explicitamente nos ADRs e mantido alinhado entre o código que publica o evento (CashFlow) e o que consome (Dashboard), servindo como fonte de verdade para os times.

### Contrato documentado do evento principal

O evento publicado na exchange `cashflow.events` é o registro `TransactionRegisteredIntegrationEvent` (ver `ArchChallenge.Contracts`).

```json
{
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "eventName": "TransactionExecuted",
  "occurredAt": "2026-04-03T10:00:00Z",
  "payload": {
    "type": "CREDIT",
    "amount": 150.00,
    "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
    "balanceAfter": 1150.00,
    "description": "Venda à vista",
    "userId": "keycloak-sub-uuid"
  }
}
```

O contrato de integração entre os serviços é definido pelo pacote compartilhado `ArchChallenge.Contracts` (referenciado pelo Dashboard), que expõe o tipo `TransactionRegisteredIntegrationEvent`.

### Convenções adotadas

- Campos em `camelCase`
- Datas no formato ISO 8601 (UTC)
- Valores monetários como `decimal` (não `float`)
- `eventId` obrigatório para idempotência no consumidor
- `eventType` obrigatório para roteamento e versionamento futuro

---

## Alternativas Consideradas

### Avro com Confluent Schema Registry

**Prós:**
- Evolução de schema controlada e retrocompatível
- Formato binário compacto — menor tamanho de mensagem
- Validação automática de contrato entre produtor e consumidor

**Contras:**
- Exige a operação de um Schema Registry como componente adicional de infraestrutura
- Maior burocracia de desenvolvimento: toda mudança de contrato exige atualização e registro do schema
- Overhead de aprendizado para times não familiarizados com Avro
- Overengineering para o volume atual de eventos (apenas um tipo de evento nesta fase)

**Descartado** por adicionar complexidade operacional desproporcional ao escopo atual.

### Protobuf

**Prós:**
- Alta performance e compactação
- Contrato fortemente tipado via arquivos `.proto`

**Contras:**
- Requer geração de código a partir de `.proto` files no pipeline de build
- Integração com RabbitMQ menos padronizada que com gRPC
- Mais complexidade sem ganho proporcional para o volume atual

**Descartado** por complexidade de setup desnecessária nesta fase.

### MessagePack

**Prós:**
- Formato binário sem schema registry
- Bom suporte no ecossistema .NET

**Contras:**
- Menos legível para debug e inspeção de mensagens
- Menor adoção e documentação em comparação ao JSON

**Descartado** em favor do JSON por maior legibilidade e simplicidade de debug.

---

## Trade-offs Documentados

| Aspecto | JSON | Avro/Protobuf |
|---|---|---|
| Legibilidade | Alta (texto legível) | Baixa (binário) |
| Tamanho da mensagem | Maior | Menor |
| Garantia de contrato | Manual (documentação) | Automática (schema registry) |
| Complexidade operacional | Baixa | Alta |
| Adequação ao volume atual | Alta | Overkill |

---

## Estratégia de Versionamento de Contrato

Para evitar breaking changes sem schema registry, adotamos as seguintes convenções:

1. Novos campos devem ser opcionais e ter valor default
2. Campos existentes não podem ser removidos ou ter o tipo alterado
3. Mudanças incompatíveis devem gerar um novo `eventName` (ex: `TransactionExecuted.v2`)
4. O campo `eventName` na mensagem permite que o consumidor ignore eventos desconhecidos

---

## Consequências

**Positivas:**
- Simplicidade de implementação e operação
- Mensagens legíveis para debug direto no RabbitMQ Management UI
- Sem dependência de componentes adicionais de infraestrutura
- Rápida adesão pelo time

**Negativas:**
- Ausência de validação automática de contrato entre produtor e consumidor
- Mensagens maiores que formatos binários (impacto negligenciável para o volume esperado)
- Risco de drift de contrato sem disciplina de documentação

---

## Referências

- [RabbitMQ — Message Properties](https://www.rabbitmq.com/publishers.html)
- [MassTransit — JSON Serialization](https://masstransit.io/documentation/configuration/serialization)
- [Martin Fowler — Tolerant Reader Pattern](https://martinfowler.com/bliki/TolerantReader.html)
