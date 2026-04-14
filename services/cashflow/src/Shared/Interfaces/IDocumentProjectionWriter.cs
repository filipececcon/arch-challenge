namespace ArchChallenge.CashFlow.Domain.Shared.Interfaces;

/// <summary>
/// Abstração de escrita de projeções no MongoDB.
/// Mantém o Outbox worker livre de dependências do MongoDB.Driver:
/// ele passa apenas o nome da coleção e o payload JSON bruto do evento.
/// </summary>
public interface IDocumentProjectionWriter
{
    /// <summary>
    /// Faz upsert do <paramref name="jsonPayload"/> na coleção indicada,
    /// normalizando campos de runtime e ajustando o <c>_id</c>.
    /// </summary>
    Task UpsertAsync(
        string            collectionName,
        string            jsonPayload,
        CancellationToken cancellationToken = default);
}
