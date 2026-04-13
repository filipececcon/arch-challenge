using System.Text.Json;

namespace ArchChallenge.CashFlow.Application.Common.Tasks;

public interface ITaskCacheService
{
    Task SetPendingAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task SetSuccessAsync(Guid taskId, JsonElement data, CancellationToken cancellationToken = default);
    Task SetFailureAsync(Guid taskId, string error, CancellationToken cancellationToken = default);
    Task<TaskResult?> GetAsync(Guid taskId, CancellationToken cancellationToken = default);
}
