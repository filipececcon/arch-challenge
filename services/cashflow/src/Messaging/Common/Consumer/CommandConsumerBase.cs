namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging.Common.Consumer;

/// <summary>
/// Base para consumers que implementam o padrão mensagem → comando MediatR.
/// A subclasse só precisa implementar <see cref="BuildCommand"/> para mapear a mensagem.
/// </summary>
public abstract class CommandConsumerBase<TMessage, TCommand>(ISender sender) : ConsumerBase<TMessage>
    where TMessage : class
    where TCommand : IRequest
{
    protected override Task ConsumeAsync(TMessage message, CancellationToken cancellationToken)
        => sender.Send(BuildCommand(message), cancellationToken);

    protected abstract TCommand BuildCommand(TMessage message);
}
