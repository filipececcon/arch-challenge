using System.Text.Json;
using ArchChallenge.CashFlow.Application.Common.Notifications;
using ArchChallenge.CashFlow.Domain.Events;
using ArchChallenge.CashFlow.Domain.Shared.Events;
using ArchChallenge.CashFlow.Domain.Interfaces;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.RegisterTransaction;

public class RegisterTransactionHandler(
    IWriteRepository<Transaction> repository,
    IOutboxRepository outboxRepository,
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
            var entity   = new Transaction(command.Type, command.Amount, command.Description);
            var response = new RegisterTransactionResult(
                entity.Id, entity.Type, entity.Amount, entity.Description, entity.CreatedAt);

            if (!entity.IsValid)
            {
                response.AddNotifications(entity.Notifications);
                return response;
            }

            // 1. Persiste a transação no PostgreSQL (write model)
            await repository.AddAsync(entity, cancellationToken);

            // 2. Cria o OutboxEvent NA MESMA TRANSAÇÃO — garante atomicidade
            //    sem 2PC. O OutboxWorkerService sincroniza para o MongoDB de
            //    forma assíncrona (consistência eventual).
            var payload = JsonSerializer.Serialize(new
            {
                id          = entity.Id.ToString(),
                type        = entity.Type.ToString(),
                amount      = entity.Amount,
                description = entity.Description,
                createdAt   = entity.CreatedAt
            });

            var outboxEvent = new OutboxEvent("TransactionRegistered", payload);
            await outboxRepository.AddAsync(outboxEvent, cancellationToken);

            // 3. SaveChanges salva Transaction + OutboxEvent atomicamente
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // 4. Commit da transação PostgreSQL
            await transaction.CommitAsync(cancellationToken);

            // 5. Publica o evento de domínio para o RabbitMQ (via MassTransit).
            //    Nota: esta publicação é best-effort após o commit; a durabilidade
            //    da mensagem é garantida pelo OutboxEvent salvo no passo 2.
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
