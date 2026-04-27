using ArchChallenge.CashFlow.Application.Abstractions.Responses;

namespace ArchChallenge.CashFlow.Application.Transactions.Enqueue;

public sealed record EnqueueTransactionResult(Guid TaskId) : IResponse;
