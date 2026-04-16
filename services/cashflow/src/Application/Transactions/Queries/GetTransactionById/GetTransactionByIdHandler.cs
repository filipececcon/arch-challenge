using ArchChallenge.CashFlow.Infrastructure.Data.Documents.Models;

namespace ArchChallenge.CashFlow.Application.Transactions.Queries.GetTransactionById;

/// <summary>
/// Leitura híbrida: Mongo (read model) primeiro; se não houver documento,
/// verifica outbox pendente para o agregado e, em caso afirmativo, lê na base relacional.
/// </summary>
public sealed class GetTransactionByIdHandler(
    IDocumentsReadRepository<TransactionDocument> documentsRepository,
    IReadRepository<Transaction>                relationalRepository,
    IOutboxRepository                           outboxRepository)
    : IRequestHandler<GetTransactionByIdQuery, GetTransactionByIdResult?>
{
    public async Task<GetTransactionByIdResult?> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var document = await documentsRepository.FindOneByIdAsync(request.Id, cancellationToken);

        if (document is not null) return GetTransactionByIdFactory.Create(document);
        
        var hasPendingSync = await outboxRepository.HasPendingForAggregateAsync(
            TransactionProcessedMessage.EventName,
            request.Id,
            cancellationToken);

        if (!hasPendingSync) return null;

        var spec = new TransactionByIdSpec(request.Id);

        var entities = await relationalRepository.FirstOrDefaultAsync(spec, cancellationToken);

        return entities is null ? null : GetTransactionByIdFactory.Create(entities);
    }
}
