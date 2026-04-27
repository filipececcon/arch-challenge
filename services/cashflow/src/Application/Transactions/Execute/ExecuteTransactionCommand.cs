using ArchChallenge.CashFlow.Domain.Enums;

namespace ArchChallenge.CashFlow.Application.Transactions.Execute;

/// <summary>
/// Fluxo síncrono com rastreamento de tarefa: executado pelo consumer do broker.
/// UoW, Outbox e TaskCache são gerenciados pelos behaviors.
/// </summary>
public record ExecuteTransactionCommand(Guid TaskId, TransactionType Type, decimal Amount, string? Description) 
    : TrackedCommand<ExecuteTransactionResult>(TaskId);
