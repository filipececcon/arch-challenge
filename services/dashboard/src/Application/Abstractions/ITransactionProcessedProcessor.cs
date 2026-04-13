using ArchChallenge.Contracts.Events;

namespace ArchChallenge.Dashboard.Application.Abstractions;

public interface ITransactionProcessedProcessor
{
    Task ProcessAsync(TransactionRegisteredIntegrationEvent message, CancellationToken cancellationToken);
}
