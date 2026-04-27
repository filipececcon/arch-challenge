using ArchChallenge.CashFlow.Application.Abstractions.Responses;
using ArchChallenge.CashFlow.Application.Abstractions.Results;

namespace ArchChallenge.CashFlow.Application.Abstractions.Commands;

// ── Fluxo de Enfileiramento ────────────────────────────────────────────────────
// Pipeline: Logging → Validation → EnqueueTaskCache → Handler
// Sem UnitOfWork, sem Outbox. O handler apenas valida e publica no broker.
// O EnqueueBehavior cuida de:
//   1. Verificar idempotência (IdempotencyKey) → short-circuit se já existe
//   2. Gerar e injetar TaskId no command
//   3. SetPending no cache antes de chamar o handler
//   4. Registrar a associação idempotencyKey → taskId após o handler

/// <summary>
/// Marcador para comandos de enfileiramento (fire-and-forget).
/// UoW e Outbox NÃO se aplicam a estes comandos.
/// O <c>EnqueueBehavior</c> gerencia TaskId e idempotência automaticamente.
/// </summary>
public interface IEnqueueCommand<TResponse> : IAuditable, IRequest<Result<TResponse>> where TResponse : class, IEnqueueResponse
{
    /// <summary>Chave de idempotência opcional. Se informada, requisições duplicadas retornam o taskId original.</summary>
    Guid? IdempotencyKey { get; }

    /// <summary>Identificador da tarefa assíncrona. Preenchido pelo <c>EnqueueBehavior</c> antes de chegar ao handler.</summary>
    Guid TaskId { get; set; }
}

/// <summary>
/// Base record para comandos de enfileiramento — evita repetir as propriedades de identidade.
/// </summary>
public abstract record EnqueueCommand<TResponse> : IEnqueueCommand<TResponse> where TResponse : class, IEnqueueResponse
{
    public string UserId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Guid? IdempotencyKey { get; init; }
    public Guid TaskId { get; set; }
}
