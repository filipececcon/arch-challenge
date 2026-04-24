using System.Text.Json;

namespace ArchChallenge.CashFlow.Application.Abstractions.Tasks;

public sealed class TaskResult
{
    public Guid TaskId { get; init; }
    public TaskStatus Status { get; init; }
    public JsonElement? Payload { get; init; }
    public string[]? Errors { get; init; }
}
