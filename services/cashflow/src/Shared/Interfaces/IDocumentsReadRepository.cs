using ArchChallenge.CashFlow.Domain.Shared.Pagination;

namespace ArchChallenge.CashFlow.Domain.Shared.Interfaces;

/// <summary>
/// Repositório de leitura genérico para documentos MongoDB.
/// Usa expressões LINQ para filtro e ordenação, mantendo a camada de
/// aplicação livre de dependências do MongoDB.Driver.
/// </summary>
/// <typeparam name="TDocument">Tipo do documento (POCO / read model).</typeparam>
public interface IDocumentsReadRepository<TDocument> where TDocument : class
{
    /// <summary>
    /// Busca por <c>_id</c> aceitando tanto UUID BinData (Standard) quanto string
    /// (documentos gravados a partir de JSON do outbox). Evita mismatch com
    /// <see cref="FindOneAsync"/> quando <c>t.Id == id</c> só serializa Guid como BinData.
    /// </summary>
    Task<TDocument?> FindOneByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Retorna o primeiro documento que satisfaz o predicado, ou null.</summary>
    Task<TDocument?> FindOneAsync(
        Expression<Func<TDocument, bool>>   predicate,
        CancellationToken                   cancellationToken = default);

    /// <summary>Lista todos os documentos que satisfazem o predicado (sem paginação).</summary>
    Task<IReadOnlyList<TDocument>> ListAsync(
        Expression<Func<TDocument, bool>>?  predicate         = null,
        Expression<Func<TDocument, object>>? orderBy          = null,
        bool                                descending        = false,
        CancellationToken                   cancellationToken = default);

    /// <summary>Lista documentos com paginação offset (page 1-based).</summary>
    Task<PagedResult<TDocument>> ListPagedAsync(
        int                                 page,
        int                                 pageSize,
        Expression<Func<TDocument, bool>>?  predicate         = null,
        Expression<Func<TDocument, object>>? orderBy          = null,
        bool                                descending        = false,
        CancellationToken                   cancellationToken = default);

    /// <summary>Conta documentos que satisfazem o predicado.</summary>
    Task<long> CountAsync(
        Expression<Func<TDocument, bool>>?  predicate         = null,
        CancellationToken                   cancellationToken = default);
}
