namespace ArchChallenge.CashFlow.Application.Abstractions.Outbox;

/// <inheritdoc />
public sealed class OutboxContext : IOutboxContext
{
    private readonly List<Domain.Shared.Entities.Outbox> _entries = [];

    public IReadOnlyList<Domain.Shared.Entities.Outbox> Entries => _entries;

    public void AddAudit(string eventName, string payload)
        => _entries.Add(Domain.Shared.Entities.Outbox.ForAudit(eventName, payload));

    public void AddMongo(string eventName, string payload)
        => _entries.Add(Domain.Shared.Entities.Outbox.ForMongo(eventName, payload));

    public void AddEvent(string eventName, string payload)
        => _entries.Add(Domain.Shared.Entities.Outbox.ForEvents(eventName, payload));
}
