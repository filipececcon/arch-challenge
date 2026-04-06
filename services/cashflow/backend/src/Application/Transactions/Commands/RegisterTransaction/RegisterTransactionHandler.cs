using ArchChallenge.CashFlow.Application.Common.Handlers;
using ArchChallenge.CashFlow.Domain.Entities;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using ArchChallenge.CashFlow.Domain.Shared.Notifications;
using MediatR;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.RegisterTransaction;

public class RegisterTransactionHandler(
    IWriteRepository<Transaction> repository,
    IPublisher publisher)
    : CommandHandlerBase<RegisterTransactionCommand, Result<RegisterTransactionResponse>>(publisher)
{
    protected override Task BeforeExecuteAsync(RegisterTransactionCommand command, CancellationToken cancellationToken)
        => Task.CompletedTask;

    protected override async Task<Result<RegisterTransactionResponse>> ExecuteAsync(
        RegisterTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var result = Transaction.Create(command.Type, command.Amount, command.Description);

        if (result.IsFailure)
            return Result<RegisterTransactionResponse>.Failure(result.Errors);

        var transaction = result.Value!;

        await repository.AddAsync(transaction, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        foreach (var @event in transaction.Events)
            RaiseEvent(@event);

        transaction.ClearEvents();

        return Result<RegisterTransactionResponse>.Success(new RegisterTransactionResponse(
            transaction.Id,
            transaction.Type,
            transaction.Amount,
            transaction.Description,
            transaction.CreatedAt));
    }

    protected override Task AfterExecuteAsync(
        RegisterTransactionCommand command,
        Result<RegisterTransactionResponse> response,
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}
