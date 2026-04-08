namespace ArchChallenge.CashFlow.Application.Common.Tasks;

public sealed class TaskResult
{
    public Guid TaskId { get; init; }
    public TaskStatus Status { get; init; }
    public object? Data { get; init; }
    public string? Error { get; init; }
}
