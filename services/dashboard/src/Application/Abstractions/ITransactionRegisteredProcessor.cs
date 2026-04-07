using ArchChallenge.Contracts.Events;

namespace ArchChallenge.Dashboard.Application.Abstractions;

public interface ITransactionRegisteredProcessor
{
    Task ProcessAsync(TransactionRegisteredIntegrationEvent message, CancellationToken cancellationToken);
}
