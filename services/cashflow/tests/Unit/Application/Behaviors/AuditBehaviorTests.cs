using ArchChallenge.CashFlow.Application.Common.Behaviors;
using ArchChallenge.CashFlow.Application.Common.Enqueue;
using ArchChallenge.CashFlow.Application.Transactions.Commands.EnqueueTransaction;
using ArchChallenge.CashFlow.Domain.Enums;
using ArchChallenge.CashFlow.Domain.Shared.Audit;
using FluentAssertions;
using MediatR;
using NSubstitute;
using MUnit = MediatR.Unit;

namespace ArchChallenge.CashFlow.Tests.Unit.Application.Behaviors;

public class AuditBehaviorTests
{
    private readonly IAuditContext _auditContext;

    public AuditBehaviorTests()
    {
        _auditContext = Substitute.For<IAuditContext>();
    }

    [Fact]
    public async Task Handle_WithCommandBase_ShouldCallSetMetadata()
    {
        var behavior  = new AuditBehavior<EnqueueTransaction, EnqueueResult>(_auditContext);
        var taskId    = Guid.NewGuid();
        var expected  = new EnqueueResult(taskId);
        var next      = Substitute.For<RequestHandlerDelegate<EnqueueResult>>();
        next(Arg.Any<CancellationToken>()).Returns(expected);

        var command = new EnqueueTransaction(TransactionType.Credit, 100m, "Test")
        {
            UserId     = "user-123",
            OccurredAt = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        var result = await behavior.Handle(command, next, CancellationToken.None);

        result.Should().Be(expected);
        _auditContext.Received(1).SetMetadata("user-123", command.OccurredAt);
    }

    [Fact]
    public async Task Handle_WithNonCommandRequest_ShouldNotCallSetMetadata()
    {
        var behavior = new AuditBehavior<NonAuditableRequest, MUnit>(_auditContext);
        var next     = Substitute.For<RequestHandlerDelegate<MUnit>>();
        next(Arg.Any<CancellationToken>()).Returns(MUnit.Value);

        await behavior.Handle(new NonAuditableRequest(), next, CancellationToken.None);

        _auditContext.DidNotReceive().SetMetadata(Arg.Any<string>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Handle_ShouldAlwaysCallNext()
    {
        var behavior = new AuditBehavior<EnqueueTransaction, EnqueueResult>(_auditContext);
        var next     = Substitute.For<RequestHandlerDelegate<EnqueueResult>>();
        next(Arg.Any<CancellationToken>()).Returns(new EnqueueResult(Guid.NewGuid()));

        await behavior.Handle(
            new EnqueueTransaction(TransactionType.Credit, 100m, null),
            next,
            CancellationToken.None);

        await next.Received(1)(Arg.Any<CancellationToken>());
    }

    private record NonAuditableRequest : IRequest<MUnit>;
}
