namespace ArchChallenge.CashFlow.Infrastructure.Data.Documents.Repositories;

/// <summary>
/// Repositório de leitura genérico para documentos MongoDB.
/// Implementa <see cref="IDocumentsReadRepository{TDocument}"/> traduzindo expressões LINQ
/// para operações do driver sem expor <c>FilterDefinition</c> / <c>SortDefinition</c>
/// nas camadas superiores.
/// </summary>
public class DocumentsReadRepository<TDocument>(IMongoCollectionResolver resolver)
    : IDocumentsReadRepository<TDocument>
    where TDocument : class
{
    private readonly IMongoCollection<TDocument> _collection = resolver.GetCollection<TDocument>();

    public async Task<TDocument?> FindOneByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var idString = id.ToString("D");
        var filter = Builders<TDocument>.Filter.Or(
            Builders<TDocument>.Filter.Eq("_id", id),
            Builders<TDocument>.Filter.Eq("_id", idString));

        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TDocument?> FindOneAsync(
        Expression<Func<TDocument, bool>> predicate,
        CancellationToken                 cancellationToken = default)
        => await _collection
            .Find(predicate)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<TDocument>> ListAsync(
        Expression<Func<TDocument, bool>>?  predicate         = null,
        Expression<Func<TDocument, object>>? orderBy          = null,
        bool                                descending        = false,
        CancellationToken                   cancellationToken = default)
    {
        var filter = predicate is not null
            ? Builders<TDocument>.Filter.Where(predicate)
            : Builders<TDocument>.Filter.Empty;

        var fluent = _collection.Find(filter);

        if (orderBy is not null)
        {
            var sort = descending
                ? Builders<TDocument>.Sort.Descending(orderBy)
                : Builders<TDocument>.Sort.Ascending(orderBy);

            fluent = fluent.Sort(sort);
        }

        return await fluent.ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<TDocument>> ListPagedAsync(
        int                                  page,
        int                                  pageSize,
        Expression<Func<TDocument, bool>>?   predicate         = null,
        Expression<Func<TDocument, object>>? orderBy           = null,
        bool                                 descending        = false,
        CancellationToken                    cancellationToken = default)
    {
        if (page < 1)    page     = 1;
        if (pageSize < 1) pageSize = 10;

        var filter = predicate is not null
            ? Builders<TDocument>.Filter.Where(predicate)
            : Builders<TDocument>.Filter.Empty;

        var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        if (totalCount == 0)
            return PagedResult<TDocument>.Empty(page, pageSize);

        var fluent = _collection.Find(filter);

        if (orderBy is not null)
        {
            var sort = descending
                ? Builders<TDocument>.Sort.Descending(orderBy)
                : Builders<TDocument>.Sort.Ascending(orderBy);

            fluent = fluent.Sort(sort);
        }

        var items = await fluent
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TDocument>(items, totalCount, page, pageSize);
    }

    public async Task<long> CountAsync(
        Expression<Func<TDocument, bool>>? predicate         = null,
        CancellationToken                  cancellationToken = default)
    {
        var filter = predicate is not null
            ? Builders<TDocument>.Filter.Where(predicate)
            : Builders<TDocument>.Filter.Empty;

        return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }
}
