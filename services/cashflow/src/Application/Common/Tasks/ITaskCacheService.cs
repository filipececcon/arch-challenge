using System.Text.Json;

namespace ArchChallenge.CashFlow.Application.Common.Tasks;

public interface ITaskCacheService
{
    Task SetPendingAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task SetSuccessAsync(Guid taskId, JsonElement data, CancellationToken cancellationToken = default);
    Task SetFailureAsync(Guid taskId, string error, CancellationToken cancellationToken = default);
    Task<TaskResult?> GetAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Retorna o taskId associado a uma idempotency key, ou null se não existir.</summary>
    Task<Guid?> GetIdempotencyAsync(Guid idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>Associa uma idempotency key a um taskId com TTL de 24 horas.</summary>
    Task SetIdempotencyAsync(Guid idempotencyKey, Guid taskId, CancellationToken cancellationToken = default);
}
