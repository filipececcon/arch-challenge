using System.Text.Json;
using ArchChallenge.CashFlow.Application.Utils;

namespace ArchChallenge.CashFlow.Application.Common.OutboxMappers;

/// <summary>
/// Serialização padrão do Mongo: JSON do tipo concreto de <typeparamref name="TProjection"/>.
/// Sobrescreva <see cref="ToMongo"/> quando o read model exigir shape diferente da entidade.
/// </summary>
public abstract class OutboxMapperBase<TCommand, TAggregate, TProjection>
    : IOutboxMapper<TCommand, TAggregate, TProjection>
    where TCommand    : CommandBase, IRequest, IAsyncCommand
    where TAggregate  : Entity, IAggregateRoot
    where TProjection : Entity
{
    public abstract string EventName { get; }

    public virtual string? ToMongo(TProjection projection, TCommand command)
        => JsonSerializer.Serialize(projection, projection.GetType(), SerializeUtils.EntityJsonOptions);

    public abstract string? ToAudit(TAggregate entity, TCommand command);

    public abstract string? ToEvents(TProjection projection, TCommand command);
}
