namespace ArchChallenge.CashFlow.Domain.Shared.Pagination;

/// <summary>
/// Resultado paginado genérico retornado pelos repositórios de documentos (Mongo).
/// </summary>
/// <typeparam name="T">Tipo do documento ou DTO retornado.</typeparam>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    long             TotalCount,
    int              Page,
    int              PageSize)
{
    public int  TotalPages      => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage     => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    public static PagedResult<T> Empty(int page, int pageSize)
        => new([], 0, page, pageSize);
}
