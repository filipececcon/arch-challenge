using ArchChallenge.CashFlow.Application.Transactions.Execute;
using ArchChallenge.CashFlow.Infrastructure.Data.Documents.Models;
using Microsoft.Extensions.Logging;

namespace ArchChallenge.CashFlow.Application.Transactions.GetById;

public sealed class GetTransactionByIdQueryHandler(
    IDocumentsReadRepository<TransactionDocument> documentsRepository,
    IReadRepository<Transaction>                relationalRepository,
    IReadRepository<Account>                    accountRepository,
    IOutboxRepository                           outboxRepository,
    ILogger<GetTransactionByIdQueryHandler>     logger)
    : IRequestHandler<GetTransactionByIdQuery, GetTransactionByIdResult?>
{
    public async Task<GetTransactionByIdResult?> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var account = await accountRepository.FirstOrDefaultAsync(
            new AccountByUserIdSpec(request.UserId), cancellationToken);

        if (account is null) return null;

        var document = await documentsRepository.FindOneByIdAsync(request.Id, cancellationToken);

        if (document is not null)
        {
            if (document.AccountId != account.Id) return null;
            logger.LogInformation("GetTransactionById: result loaded from {ReadSource}. TransactionId={TransactionId}",
                ReadSource.MongoDb, request.Id);
            return GetTransactionByIdFactory.Create(document);
        }

        var hasPendingSync = await outboxRepository.HasPendingForAggregateAsync(
            ExecuteTransactionCommandHandler.EventName, request.Id, cancellationToken: cancellationToken);

        if (!hasPendingSync)
        {
            logger.LogDebug("GetTransactionById: no Mongo document and no pending outbox; no additional read. TransactionId={TransactionId}, ReadSource={ReadSource}",
                request.Id, ReadSource.NoneNoPendingOutbox);
            return null;
        }

        var spec = new TransactionByIdSpec(request.Id);
        var entities = await relationalRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (entities is null)
        {
            logger.LogWarning("GetTransactionById: pending outbox but entity not found in relational store. TransactionId={TransactionId}, ReadSource={ReadSource}",
                request.Id, ReadSource.NonePendingOutboxEntityMissing);
            return null;
        }

        if (entities.AccountId != account.Id) return null;

        logger.LogInformation("GetTransactionById: result loaded from {ReadSource}. TransactionId={TransactionId}",
            ReadSource.Relational, request.Id);

        return GetTransactionByIdFactory.Create(entities);
    }

    private static class ReadSource
    {
        public const string MongoDb                        = nameof(MongoDb);
        public const string Relational                     = nameof(Relational);
        public const string NoneNoPendingOutbox            = nameof(NoneNoPendingOutbox);
        public const string NonePendingOutboxEntityMissing = nameof(NonePendingOutboxEntityMissing);
    }
}
