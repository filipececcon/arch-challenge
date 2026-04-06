using ArchChallenge.CashFlow.Domain.Enums;
using ArchChallenge.CashFlow.Domain.Shared.Notifications;
using MediatR;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.RegisterTransaction;

public record RegisterTransactionCommand(
    TransactionType Type,
    decimal Amount,
    string? Description) : IRequest<Result<RegisterTransactionResponse>>;

public record RegisterTransactionResponse(
    Guid Id,
    TransactionType Type,
    decimal Amount,
    string? Description,
    DateTime CreatedAt);
