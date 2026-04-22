namespace ArchChallenge.CashFlow.Application.Common.Outbox;

/// <summary>
/// Mapeia agregado e projeção para os três alvos de outbox (Mongo, audit, eventos de integração).
/// Retornar <c>null</c> em um alvo suprime a entrada correspondente.
/// </summary>
public interface IOutboxMapper<in TCommand, in TAggregate, in TProjection>
    where TCommand    : IAuditable
    where TAggregate  : Entity, IAggregateRoot
    where TProjection : Entity
{
    string EventName {get;}   
    
    string? ToMongo(TProjection projection, TCommand command);

    string? ToAudit(TAggregate entity, TCommand command);

    string? ToEvents(TProjection projection, TCommand command);
}
