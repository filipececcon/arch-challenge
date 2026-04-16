using ArchChallenge.CashFlow.Domain.Enums;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.ExecuteTransaction;

public record ExecuteTransaction(
    Guid TaskId, 
    TransactionType Type, 
    decimal Amount, 
    string? Description
) : CommandBase, IRequest, IAsyncCommand;
