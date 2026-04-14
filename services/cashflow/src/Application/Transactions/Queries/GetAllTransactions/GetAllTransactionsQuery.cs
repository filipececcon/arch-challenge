namespace ArchChallenge.CashFlow.Application.Transactions.Queries.GetAllTransactions;

/// <summary>
/// Lista transações no Mongo com filtros opcionais (query string).
/// As regras coincidem com <see cref="GetAllTransactionsQueryValidator"/>.
/// </summary>
/// <param name="Type">Tipo: <c>Credit</c> ou <c>Debit</c> (case-insensitive). Omitir = qualquer tipo.</param>
/// <param name="Active">Filtrar por <c>ST_ACTIVE</c> no read model. Omitir = qualquer.</param>
/// <param name="MinAmount">Valor mínimo (inclusive). Deve ser ≤ <paramref name="MaxAmount"/> quando ambos informados.</param>
/// <param name="MaxAmount">Valor máximo (inclusive). Deve ser ≥ <paramref name="MinAmount"/> quando ambos informados.</param>
/// <param name="CreatedFrom">Data/hora mínima de criação (UTC recomendado). Deve ser ≤ <paramref name="CreatedTo"/> quando ambos informados.</param>
/// <param name="CreatedTo">Data/hora máxima de criação (UTC recomendado).</param>
public record GetAllTransactionsQuery(
    string?    Type         = null,
    bool?      Active       = null,
    decimal?   MinAmount    = null,
    decimal?   MaxAmount    = null,
    DateTime?  CreatedFrom  = null,
    DateTime?  CreatedTo    = null)
    : IRequest<GetAllTransactionsResult>;
