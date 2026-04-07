using ArchChallenge.CashFlow.Application.Common.Notifications;
using ArchChallenge.CashFlow.Domain.Entities;
using ArchChallenge.CashFlow.Domain.Events;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using MediatR;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.RegisterTransaction;

public class RegisterTransactionHandler(
    IWriteRepository<Transaction> repository,
    IPublisher publisher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterTransactionCommand, RegisterTransactionResult>
{
    public async Task<RegisterTransactionResult> Handle(
        RegisterTransactionCommand command,
        CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var entity = new Transaction(command.Type, command.Amount, command.Description);

            var response = new RegisterTransactionResult(entity.Id, entity.Type, entity.Amount, entity.Description, entity.CreatedAt);

            if (!entity.IsValid)
            {
                response.AddNotifications(entity.Notifications);
                return response;
            }

            await repository.AddAsync(entity, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            await publisher.Publish(
                new DomainEventNotification<TransactionRegisteredEvent>(
                    new TransactionRegisteredEvent(entity)),
                cancellationToken);

            return response;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
