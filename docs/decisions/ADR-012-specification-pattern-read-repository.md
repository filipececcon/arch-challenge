# ADR-012 — Specification Pattern no ReadRepository

- **Status:** Aceito
- **Data:** 2026-04-06
- **Decisores:** Time de Arquitetura

---

## Contexto

O `IReadRepository<T>` precisava suportar queries flexíveis (filtros, ordenação, paginação) sem fixar lógica de consulta diretamente no repositório.

Duas abordagens foram consideradas:

1. Expor `IQueryable<T>` na interface do repositório, delegando a composição da query para os handlers.
2. Adotar o **Specification Pattern**, encapsulando os critérios de query em objetos dedicados.

---

## Decisão

Adotar o **Specification Pattern** com `ISpecification<T>` como contrato público e `SpecificationEvaluator<T>` como detalhe privado da camada `Data`.

O `IQueryable<T>` permanece **exclusivamente interno** à camada de infraestrutura.

---

## Estrutura

```
Shared/
└── Specifications/
    ├── ISpecification<T>       ← contrato público (critério, includes, ordenação, paginação)
    └── Specification<T>        ← classe base abstrata com métodos protegidos

Domain/
└── Specifications/
    └── TransactionSpecifications.cs  ← specs concretas de negócio

Data/
└── Specifications/
    └── SpecificationEvaluator<T>     ← internal; aplica a spec ao IQueryable<T>
```

A interface do repositório expõe apenas resultados materializados:

```csharp
public interface IReadRepository<T> where T : Entity
{
    Task<T?>              GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<T?>              FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T>? spec = null, CancellationToken ct = default);
    Task<int>             CountAsync(ISpecification<T>? spec = null, CancellationToken ct = default);
}
```

Handlers declaram *o que querem* através de specs; o repositório resolve *como buscar*:

```csharp
// Application — declara intenção
var spec = new TransactionsOrderedByDateSpec();
var transactions = await repository.ListAsync(spec, cancellationToken);

// Data — resolve internamente
query = SpecificationEvaluator<T>.GetQuery(_dbSet.AsNoTracking(), spec);
return await query.ToListAsync(cancellationToken);
```

---

## Alternativas Consideradas

### Expor `IQueryable<T>` na interface do repositório

**Prós:**
- Máxima flexibilidade para compor queries nos handlers

**Contras:**
- Vaza o contrato do ORM (EF Core) para a camada de Application
- Handlers passam a depender implicitamente de um provider de LINQ ativo
- Testes unitários exigem InMemory/SQLite provider ao invés de mocks simples
- O ciclo de vida do `DbContext` precisa ser gerenciado fora do repositório
- Qualquer handler pode contornar a abstração e escrever queries arbitrárias

### `ListAsync` sem parâmetros (implementação original)

**Prós:**
- Interface simples

**Contras:**
- Sem suporte a filtros, ordenação customizada ou paginação
- Lógica de ordenação hardcoded no repositório genérico

---

## Consequências

**Positivas:**
- A camada de Application não tem conhecimento de EF Core ou `IQueryable<T>`
- Handlers são testáveis com mocks simples de `IReadRepository<T>`
- Novas queries são adicionadas criando specs, sem modificar o repositório
- `SpecificationEvaluator<T>` é `internal`, garantindo que o `IQueryable` nunca vaze

**Negativas:**
- Requer a criação de uma classe de spec para cada novo cenário de query
- Composição de múltiplos critérios exige specs compostas ou herança

---

## Referências

- [Specification Pattern — Martin Fowler](https://martinfowler.com/apsupp/spec.pdf)
- [Ardalis.Specification — implementação de referência](https://github.com/ardalis/Specification)
