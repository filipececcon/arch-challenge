namespace ArchChallenge.CashFlow.Application.Common.Events;

/// <summary>
/// Base record para mensagens de eventos publicadas no message broker.
/// Garante que todo evento de saída carregue um identificador único,
/// o instante de ocorrência e o nome canônico do evento.
/// </summary>
public abstract record MessageBase(string EventName)
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

