using System.Text.Json;
using ArchChallenge.CashFlow.Application.Common.Interfaces;
using ArchChallenge.CashFlow.Application.Common.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using TaskStatus = ArchChallenge.CashFlow.Application.Common.Tasks.TaskStatus;

namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Caching.Services;

public sealed class TaskCacheService(IDistributedCache cache) : ITaskCacheService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public Task SetPendingAsync(Guid taskId, CancellationToken cancellationToken = default)
        => SetAsync(taskId, new TaskResult { TaskId = taskId, Status = TaskStatus.Pending }, cancellationToken);

    public Task SetSuccessAsync(Guid taskId, JsonElement data, CancellationToken cancellationToken = default)
        => SetAsync(taskId, new TaskResult { TaskId = taskId, Status = TaskStatus.Success, Payload = data }, cancellationToken);

    public Task SetFailureAsync(Guid taskId, string error, CancellationToken cancellationToken = default)
        => SetAsync(taskId, new TaskResult { TaskId = taskId, Status = TaskStatus.Failure, Error = error }, cancellationToken);

    public async Task<TaskResult?> GetAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var bytes = await cache.GetAsync(CacheKey(taskId), cancellationToken);
        return bytes is null ? null : JsonSerializer.Deserialize<TaskResult>(bytes, JsonOptions);
    }

    private Task SetAsync(Guid taskId, TaskResult result, CancellationToken cancellationToken)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(result, JsonOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Ttl
        };
        return cache.SetAsync(CacheKey(taskId), bytes, options, cancellationToken);
    }

    private static string CacheKey(Guid taskId) => $"task:{taskId}";
}
