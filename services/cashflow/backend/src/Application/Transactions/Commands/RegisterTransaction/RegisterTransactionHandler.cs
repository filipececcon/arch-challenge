using ArchChallenge.CashFlow.Application.Common.Handlers;
using ArchChallenge.CashFlow.Domain.Entities;
using ArchChallenge.CashFlow.Domain.Events;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using MediatR;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.RegisterTransaction;

public class RegisterTransactionHandler(
    IWriteRepository<Transaction> repository,
    IPublisher publisher,
    IUnitOfWork unitOfWork)
    : CommandHandlerBase<RegisterTransactionCommand, RegisterTransactionResult, TransactionRegisteredEvent>(publisher, unitOfWork)
{
    protected override async Task<RegisterTransactionResult> ExecuteAsync(
        RegisterTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var entity = new Transaction(command.Type, command.Amount, command.Description);

        var response = new RegisterTransactionResult(entity.Id, entity.Type, entity.Amount, entity.Description, entity.CreatedAt);

        if (entity.IsValid)
        {
            await repository.AddAsync(entity, cancellationToken);
        }
        else
        {
            response.AddNotifications(entity.Notifications);
        }

        return response;
    }
}
