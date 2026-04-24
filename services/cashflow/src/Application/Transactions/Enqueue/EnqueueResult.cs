using ArchChallenge.CashFlow.Application.Abstractions.Responses;

namespace ArchChallenge.CashFlow.Application.Transactions.Enqueue;

public sealed record EnqueueResult(Guid TaskId) : IResponse;
