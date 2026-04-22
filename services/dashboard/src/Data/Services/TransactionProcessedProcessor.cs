using System.Globalization;
using ArchChallenge.Contracts.Events;
using ArchChallenge.Dashboard.Infrastructure.Data.Documents;
using MongoDB.Driver;

namespace ArchChallenge.Dashboard.Infrastructure.Data.Services;

public class TransactionProcessedProcessor(IMongoDatabase database) : ITransactionProcessedProcessor
{
    private readonly IMongoCollection<StatementLineDocument> _statement =
        database.GetCollection<StatementLineDocument>(MongoDashboardCollections.StatementLines);
    private readonly IMongoCollection<DailyConsolidationDocument> _daily =
        database.GetCollection<DailyConsolidationDocument>(MongoDashboardCollections.DailyConsolidations);

    public async Task ProcessAsync(TransactionRegisteredIntegrationEvent message, CancellationToken cancellationToken)
    {
        var occurredUtc = message.OccurredAt.Kind == DateTimeKind.Utc
            ? message.OccurredAt
            : message.OccurredAt.ToUniversalTime();

        var dayId = DateOnly.FromDateTime(occurredUtc).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var type  = message.Payload.Type.ToUpperInvariant();

        var compositeDailyId = $"{message.Payload.AccountId:N}|{dayId}";

        // Upsert da linha de extrato — idempotente pela chave _id = EventId (MongoDB garante unicidade).
        var line = new StatementLineDocument
        {
            Id         = message.EventId,
            AccountId  = message.Payload.AccountId,
            UserId     = message.Payload.UserId,
            Day        = dayId,
            OccurredAt = occurredUtc,
            Type       = type,
            Amount     = message.Payload.Amount
        };

        await _statement.ReplaceOneAsync(
            Builders<StatementLineDocument>.Filter.Eq(l => l.Id, message.EventId),
            line,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);

        // Recalcula o consolidado do dia a partir das linhas — fonte única de verdade.
        var dayLines = await _statement
            .Find(
                Builders<StatementLineDocument>.Filter.And(
                    Builders<StatementLineDocument>.Filter.Eq(l => l.Day, dayId),
                    Builders<StatementLineDocument>.Filter.Eq(l => l.AccountId, message.Payload.AccountId)))
            .ToListAsync(cancellationToken);

        var totalCredits = dayLines.Where(l => l.Type == "CREDIT").Sum(l => l.Amount);
        var totalDebits  = dayLines.Where(l => l.Type == "DEBIT").Sum(l => l.Amount);

        var update = Builders<DailyConsolidationDocument>.Update
            .Set(d => d.TotalCredits, totalCredits)
            .Set(d => d.TotalDebits,  totalDebits)
            .Set(d => d.UpdatedAt,    DateTime.UtcNow)
            .Set(d => d.AccountId,    message.Payload.AccountId)
            .Set(d => d.UserId,       message.Payload.UserId)
            .Set(d => d.Day,          dayId);

        await _daily.UpdateOneAsync(
            Builders<DailyConsolidationDocument>.Filter.Eq(d => d.Id, compositeDailyId),
            update,
            new UpdateOptions { IsUpsert = true },
            cancellationToken);
    }
}
