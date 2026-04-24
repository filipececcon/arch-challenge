namespace ArchChallenge.CashFlow.Application.Common.Outbox;

public class OutboxWriter<TCommand, TAggregate, TProjection>(
    IOutboxRepository outboxRepository,
    IOutboxMapper<TCommand, TAggregate, TProjection> mapper)
    where TCommand    : IAuditable
    where TAggregate  : Entity, IAggregateRoot
    where TProjection : Entity
{
    public async Task WriteAsync(
        TAggregate entity, TProjection projection,
        TCommand command, CancellationToken cancellationToken)
    {
        var name = mapper.EventName;

        var mongo = mapper.ToMongo(projection, command);
        if (mongo is not null)
            await outboxRepository.AddAsync(Domain.Shared.Entities.Outbox.ForMongo(name, mongo), cancellationToken);

        var audit = mapper.ToAudit(entity, command);
        if (audit is not null)
            await outboxRepository.AddAsync(Domain.Shared.Entities.Outbox.ForAudit(name, audit), cancellationToken);

        var events = mapper.ToEvents(projection, command);
        if (events is not null)
            await outboxRepository.AddAsync(Domain.Shared.Entities.Outbox.ForEvents(name, events), cancellationToken);
    }
}