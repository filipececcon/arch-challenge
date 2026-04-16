using ArchChallenge.Contracts.Events;

namespace ArchChallenge.Dashboard.Infrastructure.Data;

/// <summary>
/// Projeta um evento de integração recebido do CashFlow no read model do Dashboard.
/// Pertence à camada de dados: é uma operação de escrita no MongoDB, não uma abstração de consulta.
/// </summary>
public interface ITransactionProcessedProcessor
{
    Task ProcessAsync(TransactionRegisteredIntegrationEvent message, CancellationToken cancellationToken);
}
