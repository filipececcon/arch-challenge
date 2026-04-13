namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging.Common.Consumer;

/// <summary>
/// Base para consumers que desacopla a lógica de negócio do ConsumeContext do MassTransit.
/// Subclasses implementam apenas <see cref="ConsumeAsync"/> sem depender de tipos do MassTransit.
/// </summary>
public abstract class ConsumerBase<TMessage> : IConsumer<TMessage>
    where TMessage : class
{
    public Task Consume(ConsumeContext<TMessage> context)
        => ConsumeAsync(context.Message, context.CancellationToken);

    protected abstract Task ConsumeAsync(TMessage message, CancellationToken cancellationToken);
}
