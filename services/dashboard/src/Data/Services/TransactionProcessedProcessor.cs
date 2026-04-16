using System.Globalization;
using ArchChallenge.Contracts.Events;
using ArchChallenge.Dashboard.Application.Abstractions;
using ArchChallenge.Dashboard.Data.Documents;
using MongoDB.Driver;

namespace ArchChallenge.Dashboard.Data.Services;

public class TransactionProcessedProcessor : ITransactionProcessedProcessor
{
    private readonly IMongoCollection<DailyConsolidationDocument> _daily;
    private readonly IMongoCollection<ProcessedIntegrationEventDocument> _processed;

    public TransactionProcessedProcessor(IMongoDatabase database)
    {
        _daily = database.GetCollection<DailyConsolidationDocument>(MongoDashboardCollections.DailyConsolidations);
        _processed = database.GetCollection<ProcessedIntegrationEventDocument>(MongoDashboardCollections.ProcessedIntegrationEvents);
    }

    public async Task ProcessAsync(TransactionRegisteredIntegrationEvent message, CancellationToken cancellationToken)
    {
        var filterProcessed = Builders<ProcessedIntegrationEventDocument>.Filter.Eq(e => e.Id, message.EventId);
        var updateProcessed = Builders<ProcessedIntegrationEventDocument>.Update
            .SetOnInsert(e => e.ProcessedAt, DateTime.UtcNow);

        var ack = await _processed.UpdateOneAsync(
            filterProcessed,
            updateProcessed,
            new UpdateOptions { IsUpsert = true },
            cancellationToken);

        if (ack.UpsertedId is null && ack.MatchedCount > 0)
            return;

        if (ack.UpsertedId is null)
            throw new InvalidOperationException("Evento não pôde ser registrado de forma idempotente.");

        var occurredUtc = message.OccurredAt.Kind == DateTimeKind.Utc
            ? message.OccurredAt
            : message.OccurredAt.ToUniversalTime();

        var day = DateOnly.FromDateTime(occurredUtc);
        var dayId = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var amount = message.Payload.Amount;
        decimal incCredit = 0, incDebit = 0;
        if (string.Equals(message.Payload.Type, "CREDIT", StringComparison.OrdinalIgnoreCase))
            incCredit = amount;
        else if (string.Equals(message.Payload.Type, "DEBIT", StringComparison.OrdinalIgnoreCase))
            incDebit = amount;

        var filterDay = Builders<DailyConsolidationDocument>.Filter.Eq(d => d.Id, dayId);
        var updateDay = Builders<DailyConsolidationDocument>.Update
            .Inc(d => d.TotalCredits, incCredit)
            .Inc(d => d.TotalDebits, incDebit)
            .Set(d => d.UpdatedAt, DateTime.UtcNow);

        await _daily.UpdateOneAsync(filterDay, updateDay, new UpdateOptions { IsUpsert = true }, cancellationToken);
    }
}
