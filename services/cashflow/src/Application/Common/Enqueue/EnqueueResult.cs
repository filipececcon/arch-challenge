using ArchChallenge.CashFlow.Application.Common.Responses;

namespace ArchChallenge.CashFlow.Application.Common.Enqueue;

public sealed record EnqueueResult(Guid TaskId) : IResponse;
