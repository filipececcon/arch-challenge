using ArchChallenge.CashFlow.Domain.Shared.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Logging.Workers;

/// <summary>
/// Contrato que todo worker de outbox deve implementar.
///
/// Define o ciclo de polling em três fases bem delimitadas, permitindo que
/// variações (Mongo, Events, auditoria/immudb) compartilhem o mesmo
/// template de orquestração em <see cref="OutboxWorkerBase"/>.
/// </summary>
public interface IOutboxWorker
{
    /// <summary>Busca lote de registros pendentes a partir do repositório.</summary>
    Task<IReadOnlyList<Outbox>> FetchPendingAsync(
        IServiceScope     scope,
        CancellationToken cancellationToken);

    /// <summary>Processa um único registro (projeção ou auditoria).</summary>
    Task ProcessSingleAsync(
        Outbox            outbox,
        CancellationToken cancellationToken);

    /// <summary>Persiste as alterações de estado dos registros processados.</summary>
    Task PersistChangesAsync(
        IServiceScope     scope,
        CancellationToken cancellationToken);
}

