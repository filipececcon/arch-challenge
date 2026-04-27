namespace ArchChallenge.CashFlow.Application.Abstractions.Commands;

// ── Fluxo de Enfileiramento ────────────────────────────────────────────────────
// Pipeline: Logging → Validation → Handler
// Sem UnitOfWork, sem Outbox. O handler apenas valida e publica no broker.

/// <summary>
/// Marcador para comandos de enfileiramento (fire-and-forget).
/// UoW e Outbox NÃO se aplicam a estes comandos.
/// </summary>
public interface IEnqueueCommand<out TResponse> : IAuditable, IRequest<TResponse> where TResponse : class;

/// <summary>
/// Base record para comandos de enfileiramento — evita repetir as propriedades de identidade.
/// </summary>
public abstract record EnqueueCommand<TResponse> : IEnqueueCommand<TResponse> where TResponse : class
{
    public string UserId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
