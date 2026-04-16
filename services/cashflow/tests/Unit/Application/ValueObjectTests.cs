using System.Text.Json;
using ArchChallenge.CashFlow.Application.Common.Tasks;
using ArchChallenge.CashFlow.Application.Transactions.Queries.GetAllTransactions;
using ArchChallenge.CashFlow.Application.Transactions.Queries.GetTransactionById;
using ArchChallenge.CashFlow.Domain.Shared.Audit;
using FluentAssertions;
using AppTaskStatus = ArchChallenge.CashFlow.Application.Common.Tasks.TaskStatus;

namespace ArchChallenge.CashFlow.Tests.Unit.Application;

public class ValueObjectTests
{
    [Fact]
    public void TaskResult_ShouldSetAllProperties()
    {
        var taskId  = Guid.NewGuid();
        var payload = JsonSerializer.SerializeToElement(new { amount = 100 });

        var result = new TaskResult
        {
            TaskId  = taskId,
            Status  = AppTaskStatus.Success,
            Payload = payload,
            Error   = null
        };

        result.TaskId.Should().Be(taskId);
        result.Status.Should().Be(AppTaskStatus.Success);
        result.Payload.Should().NotBeNull();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void TaskResult_WithFailureStatus_ShouldHoldErrorMessage()
    {
        var result = new TaskResult
        {
            TaskId = Guid.NewGuid(),
            Status = AppTaskStatus.Failure,
            Error  = "Domain error occurred"
        };

        result.Status.Should().Be(AppTaskStatus.Failure);
        result.Error.Should().Be("Domain error occurred");
        result.Payload.Should().BeNull();
    }

    [Fact]
    public void TaskResult_WithPendingStatus_ShouldHaveNoPayloadOrError()
    {
        var result = new TaskResult
        {
            TaskId = Guid.NewGuid(),
            Status = AppTaskStatus.Pending
        };

        result.Status.Should().Be(AppTaskStatus.Pending);
        result.Payload.Should().BeNull();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void GetAllTransactionsResult_ShouldGroupTransactions()
    {
        var transactions = new[]
        {
            new GetTransactionByIdResult(Guid.NewGuid(), "Credit", 100m, null, DateTime.UtcNow, true),
            new GetTransactionByIdResult(Guid.NewGuid(), "Debit",  50m,  "Test", DateTime.UtcNow, true)
        };

        var result = new GetAllTransactionsResult(transactions);

        result.Transactions.Should().HaveCount(2);
        result.Transactions.Should().Contain(t => t.Type == "Credit");
        result.Transactions.Should().Contain(t => t.Type == "Debit");
    }

    [Fact]
    public void GetTransactionByIdResult_ShouldSetAllProperties()
    {
        var id        = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var result = new GetTransactionByIdResult(id, "Credit", 250m, "Payment", createdAt, true);

        result.Id.Should().Be(id);
        result.Type.Should().Be("Credit");
        result.Amount.Should().Be(250m);
        result.Description.Should().Be("Payment");
        result.CreatedAt.Should().Be(createdAt);
        result.Active.Should().BeTrue();
    }

    [Fact]
    public void AuditEntry_ShouldSetAllProperties()
    {
        var occurredAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        var entry = new AuditEntry(
            AuditId:       "audit-1",
            AggregateType: "Transaction",
            AggregateId:   "agg-1",
            EventName:     "TransactionProcessed",
            UserId:        "user-1",
            OccurredAt:    occurredAt,
            Payload:       "{\"amount\":100}");

        entry.AuditId.Should().Be("audit-1");
        entry.AggregateType.Should().Be("Transaction");
        entry.AggregateId.Should().Be("agg-1");
        entry.EventName.Should().Be("TransactionProcessed");
        entry.UserId.Should().Be("user-1");
        entry.OccurredAt.Should().Be(occurredAt);
        entry.Payload.Should().Be("{\"amount\":100}");
    }
}
