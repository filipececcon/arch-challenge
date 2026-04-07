using ArchChallenge.CashFlow.Application.Common.Responses;
using ArchChallenge.CashFlow.Domain.Enums;
using MediatR;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.RegisterTransaction;

public record RegisterTransactionCommand(
    TransactionType Type,
    decimal Amount,
    string? Description) : IRequest<RegisterTransactionResult>;


