using System.Text.Json;
using ArchChallenge.CashFlow.Application.Utils;
using ArchChallenge.CashFlow.Domain.Shared.Audit;
using ArchChallenge.CashFlow.Domain.Shared.Entities;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using ArchChallenge.CashFlow.Domain.Shared.Projection;

namespace ArchChallenge.CashFlow.Application.Common.Audit;

public sealed class AuditContext : IAuditContext
{
    private string?         _userId;
    private DateTime        _occurredAt;
    private string?         _eventName;
    private IAggregateRoot? _aggregate;
    private bool            _persisted;

    public void SetMetadata(string userId, DateTime occurredAt)
    {
        _userId     = userId;
        _occurredAt = occurredAt;
    }

    /// <inheritdoc/>
    public void Capture(IAggregateRoot aggregate, string eventName)
    {
        _aggregate = aggregate;
        _eventName = eventName;
    }

    /// <inheritdoc/>
    public bool TryBuildAuditOutboxPayload(out string eventName, out string payloadJson)
    {
        eventName   = _eventName ?? string.Empty;
        payloadJson = string.Empty;

        if (_persisted || _aggregate is null || string.IsNullOrWhiteSpace(_eventName))
            return false;

        // O snapshot é tirado aqui, chamado pela UoW *após* o SaveChangesAsync principal,
        // garantindo que o Id gerado pelo banco já esteja preenchido na entidade.
        var aggregateId = _aggregate is Entity e ? e.Id : Guid.Empty;

        // Mesma projeção que outbox / task cache: tipo concreto + strip de Flunt/base Entity.
        var stateElement = JsonSerializer.SerializeToElement(_aggregate, _aggregate.GetType(), SerializeUtils.EntityJsonOptions);
        stateElement = EntityProjectionJson.RemoveRuntimeFields(stateElement);

        payloadJson = JsonSerializer.Serialize(new
        {
            auditId       = Guid.NewGuid().ToString("D"),
            userId        = _userId ?? string.Empty,
            occurredAt    = _occurredAt,
            eventName     = _eventName,
            aggregateType = _aggregate.GetType().Name,
            aggregateId   = aggregateId.ToString("D"),
            state         = stateElement
        }, SerializeUtils.EntityJsonOptions);

        return true;
    }

    public void NotifyPersisted()
    {
        _userId     = null;
        _occurredAt = default;
        _eventName  = null;
        _aggregate  = null;
        _persisted  = true;
    }
}
