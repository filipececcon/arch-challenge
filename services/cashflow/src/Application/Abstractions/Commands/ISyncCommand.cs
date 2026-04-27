using ArchChallenge.CashFlow.Application.Abstractions.Results;

namespace ArchChallenge.CashFlow.Application.Abstractions.Commands;

// ── Fluxo Síncrono ────────────────────────────────────────────────────────────
// Pipeline: Logging → Validation → TaskCache¹ → UnitOfWork → Outbox → Handler
// ¹ TaskCache aplica-se apenas a ITrackedCommand

/// <summary>
/// Marcador para comandos do fluxo síncrono.
/// <c>UnitOfWorkBehavior</c> e <c>OutboxBehavior</c> ativam-se para todo
/// request que implemente esta interface — independente da origem (API ou consumer).
/// </summary>
public interface ISyncCommand : IAuditable { }

/// <summary>Comando síncrono tipado que retorna <see cref="Result{TResponse}"/>.</summary>
public interface ISyncCommand<TResponse> : ISyncCommand, IRequest<Result<TResponse>> where TResponse : class;

/// <summary>
/// Base record para comandos síncronos — evita repetir as propriedades de identidade.
/// </summary>
public abstract record SyncCommand<TResponse> : ISyncCommand<TResponse> where TResponse : class
{
    public string UserId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}