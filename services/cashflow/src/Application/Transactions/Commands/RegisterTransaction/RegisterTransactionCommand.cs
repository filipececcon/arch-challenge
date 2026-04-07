using ArchChallenge.CashFlow.Domain.Enums;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.RegisterTransaction;

public record RegisterTransactionCommand(
    TransactionType Type,
    decimal Amount,
    string? Description) : IRequest<RegisterTransactionResult>;


