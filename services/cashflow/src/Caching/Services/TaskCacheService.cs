using System.Text.Json;
using ArchChallenge.CashFlow.Application.Common.Interfaces;
using ArchChallenge.CashFlow.Application.Common.Tasks;
using ArchChallenge.CashFlow.Domain.Shared.Projection;
using Microsoft.Extensions.Caching.Distributed;
using TaskStatus = ArchChallenge.CashFlow.Application.Common.Tasks.TaskStatus;

namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Caching.Services;

public sealed class TaskCacheService(IDistributedCache cache) : ITaskCacheService
{
    private static readonly TimeSpan Ttl             = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan IdempotencyTtl   = TimeSpan.FromHours(24);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public Task SetPendingAsync(Guid taskId, CancellationToken cancellationToken = default)
        => SetAsync(taskId, new TaskResult { TaskId = taskId, Status = TaskStatus.Pending }, cancellationToken);

    public Task SetSuccessAsync(Guid taskId, JsonElement data, CancellationToken cancellationToken = default)
    {
        var payload = EntityProjectionJson.RemoveRuntimeFields(data);
        return SetAsync(taskId, new TaskResult { TaskId = taskId, Status = TaskStatus.Success, Payload = payload }, cancellationToken);
    }

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

    public async Task<Guid?> GetIdempotencyAsync(Guid idempotencyKey, CancellationToken cancellationToken = default)
    {
        var bytes = await cache.GetAsync(IdempotencyKey(idempotencyKey), cancellationToken);
        return bytes is null ? null : new Guid(bytes);
    }

    public Task SetIdempotencyAsync(Guid idempotencyKey, Guid taskId, CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = IdempotencyTtl
        };
        return cache.SetAsync(IdempotencyKey(idempotencyKey), taskId.ToByteArray(), options, cancellationToken);
    }

    private static string CacheKey(Guid taskId)         => $"task:{taskId}";
    private static string IdempotencyKey(Guid key)      => $"idempotency:{key}";
}
