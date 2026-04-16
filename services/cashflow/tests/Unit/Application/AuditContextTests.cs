using ArchChallenge.CashFlow.Application.Common.Audit;
using ArchChallenge.CashFlow.Domain.Entities;
using ArchChallenge.CashFlow.Domain.Enums;
using FluentAssertions;

namespace ArchChallenge.CashFlow.Tests.Unit.Application;

public class AuditContextTests
{
    private readonly AuditContext _context = new();

    [Fact]
    public void TryBuildAuditOutboxPayload_WithoutCapture_ShouldReturnFalse()
    {
        var success = _context.TryBuildAuditOutboxPayload(out var eventName, out var payload);

        success.Should().BeFalse();
        eventName.Should().BeEmpty();
        payload.Should().BeEmpty();
    }

    [Fact]
    public void TryBuildAuditOutboxPayload_AfterCapture_ShouldReturnTrueWithPayload()
    {
        var entity    = new Transaction(TransactionType.Credit, 100m, "Test");
        var eventName = "TransactionProcessed";

        _context.SetMetadata("user-1", DateTime.UtcNow);
        _context.Capture(entity, eventName);

        var success = _context.TryBuildAuditOutboxPayload(out var capturedEvent, out var payloadJson);

        success.Should().BeTrue();
        capturedEvent.Should().Be(eventName);
        payloadJson.Should().NotBeNullOrEmpty();
        payloadJson.Should().Contain("TransactionProcessed");
        payloadJson.Should().Contain("user-1");
    }

    [Fact]
    public void TryBuildAuditOutboxPayload_AfterNotifyPersisted_ShouldReturnFalse()
    {
        var entity = new Transaction(TransactionType.Debit, 50m);

        _context.SetMetadata("user-1", DateTime.UtcNow);
        _context.Capture(entity, "TransactionProcessed");
        _context.TryBuildAuditOutboxPayload(out _, out _);
        _context.NotifyPersisted();

        var success = _context.TryBuildAuditOutboxPayload(out var eventName, out var payload);

        success.Should().BeFalse();
        eventName.Should().BeEmpty();
        payload.Should().BeEmpty();
    }

    [Fact]
    public void TryBuildAuditOutboxPayload_BeforeNotifyPersisted_ShouldReturnTrueOnEveryCall()
    {
        var entity = new Transaction(TransactionType.Credit, 200m);

        _context.SetMetadata("user-x", DateTime.UtcNow);
        _context.Capture(entity, "TransactionProcessed");

        var firstCall  = _context.TryBuildAuditOutboxPayload(out _, out _);
        var secondCall = _context.TryBuildAuditOutboxPayload(out _, out _);

        firstCall.Should().BeTrue();
        secondCall.Should().BeTrue("o payload pode ser construído várias vezes até NotifyPersisted ser chamado");
    }

    [Fact]
    public void TryBuildAuditOutboxPayload_ShouldContainAggregateAndUserId()
    {
        var entity    = new Transaction(TransactionType.Credit, 300m, "Audit test");
        var userId    = "user-audit";
        var occurredAt = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        _context.SetMetadata(userId, occurredAt);
        _context.Capture(entity, "TransactionProcessed");

        _context.TryBuildAuditOutboxPayload(out _, out var payloadJson);

        payloadJson.Should().Contain(userId);
        payloadJson.Should().Contain("Transaction");
        payloadJson.Should().Contain("2026");
    }

    [Fact]
    public void NotifyPersisted_ShouldClearAllState()
    {
        var entity = new Transaction(TransactionType.Credit, 100m);

        _context.SetMetadata("user-1", DateTime.UtcNow);
        _context.Capture(entity, "TransactionProcessed");
        _context.NotifyPersisted();

        var success = _context.TryBuildAuditOutboxPayload(out var eventName, out var payload);

        success.Should().BeFalse();
        eventName.Should().BeEmpty();
        payload.Should().BeEmpty();
    }
}
