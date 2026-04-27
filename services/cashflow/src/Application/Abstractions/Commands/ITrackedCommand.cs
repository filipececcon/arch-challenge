namespace ArchChallenge.CashFlow.Application.Abstractions.Commands;

// ── Fluxo Síncrono com rastreamento de tarefa ─────────────────────────────────
// Subconjunto do fluxo síncrono: o TaskCacheBehavior ativa-se para estes comandos
// e atualiza o status no Redis (Pending → Success / Failure) após a transação.

/// <summary>
/// Marcador para comandos síncronos que rastreiam uma tarefa via <c>TaskId</c>.
/// Herda <see cref="ISyncCommand"/>, portanto UoW e Outbox também se aplicam.
/// </summary>
public interface ITrackedCommand : ISyncCommand
{
    Guid TaskId { get; }
}

/// <summary>Versão tipada de <see cref="ITrackedCommand"/>.</summary>
public interface ITrackedCommand<TResponse> : ITrackedCommand, ISyncCommand<TResponse>
    where TResponse : class;

/// <summary>Base record para comandos com rastreamento de tarefa.</summary>
public abstract record TrackedCommand<TResponse>(Guid TaskId) : ITrackedCommand<TResponse>
    where TResponse : class
{
    public string UserId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
