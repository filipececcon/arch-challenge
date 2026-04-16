using ArchChallenge.CashFlow.Domain.Shared.Events;
using FluentAssertions;

namespace ArchChallenge.CashFlow.Tests.Unit.Domain;

public class EventBaseTests
{
    [Fact]
    public void OutboxEvent_Create_ShouldSetPropertiesCorrectly()
    {
        var evt = new OutboxEvent("TransactionProcessed", "{\"id\":\"abc\"}");

        evt.EventType.Should().Be("TransactionProcessed");
        evt.Payload.Should().Be("{\"id\":\"abc\"}");
        evt.Processed.Should().BeFalse();
        evt.ProcessedAt.Should().BeNull();
        evt.RetryCount.Should().Be(0);
        evt.Id.Should().NotBeEmpty();
        evt.Active.Should().BeTrue();
    }

    [Fact]
    public void OutboxEvent_MarkProcessed_ShouldSetProcessedAndTimestamp()
    {
        var evt = new OutboxEvent("TransactionProcessed", "{}");

        evt.MarkProcessed();

        evt.Processed.Should().BeTrue();
        evt.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void OutboxEvent_IncrementRetry_ShouldIncrementCounter()
    {
        var evt = new OutboxEvent("TransactionProcessed", "{}");

        evt.IncrementRetry();
        evt.IncrementRetry();
        evt.IncrementRetry();

        evt.RetryCount.Should().Be(3);
    }

    [Fact]
    public void AuditEvent_Create_ShouldSetPropertiesCorrectly()
    {
        var evt = new AuditEvent("TransactionProcessed", "{\"auditId\":\"x\"}");

        evt.EventType.Should().Be("TransactionProcessed");
        evt.Payload.Should().Be("{\"auditId\":\"x\"}");
        evt.Processed.Should().BeFalse();
        evt.RetryCount.Should().Be(0);
    }

    [Fact]
    public void AuditEvent_MarkProcessed_ShouldSetProcessedAndTimestamp()
    {
        var evt = new AuditEvent("TransactionProcessed", "{}");

        evt.MarkProcessed();

        evt.Processed.Should().BeTrue();
        evt.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void AuditEvent_IncrementRetry_ShouldIncrementCounter()
    {
        var evt = new AuditEvent("TransactionProcessed", "{}");

        evt.IncrementRetry();

        evt.RetryCount.Should().Be(1);
    }
}
