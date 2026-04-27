namespace ArchChallenge.CashFlow.Application.Abstractions.Outbox;

/// <summary>
/// Contexto scoped onde handlers registram entradas de outbox.
/// Os behaviors de pipeline (Audit, Mongo, Events) leem e persistem
/// as entradas após o handler, dentro da mesma transação.
/// </summary>
public interface IOutboxContext
{
    IReadOnlyList<Domain.Shared.Entities.Outbox> Entries { get; }

    void AddAudit(string eventName, string payload);
    void AddMongo(string eventName, string payload);
    void AddEvent(string eventName, string payload);
}
