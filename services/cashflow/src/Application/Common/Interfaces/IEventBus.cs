namespace ArchChallenge.CashFlow.Application.Common.Interfaces;

public interface IEventBus
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Publica uma mensagem não tipada em tempo de compilação (uso pelo <c>OutboxEventsWorkerService</c>).
    /// Necessário para despachar eventos cujo CLR type só é conhecido em runtime.
    /// </summary>
    Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken = default);
}
