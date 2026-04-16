namespace ArchChallenge.Dashboard.Application.Statement.ListStatementLines;

/// <param name="From">Início do intervalo (inclusive). Nulo = sem limite inferior.</param>
/// <param name="To">Fim do intervalo (inclusive). Nulo = sem limite superior.</param>
/// <param name="Type">Filtro opcional por tipo: CREDIT ou DEBIT.</param>
/// <param name="Page">Página (base 1).</param>
/// <param name="PageSize">Itens por página (máx. 200).</param>
public record ListStatementLinesQuery(
    DateOnly? From,
    DateOnly? To,
    string? Type,
    int Page = 1,
    int PageSize = 50) : IRequest<StatementPageDto>;
