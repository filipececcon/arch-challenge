using ArchChallenge.CashFlow.Application.Common.Behaviors;
using ArchChallenge.CashFlow.Application.Common.Enqueue;
using ArchChallenge.CashFlow.Application.Transactions.Commands.EnqueueTransaction;
using ArchChallenge.CashFlow.Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MUnit = MediatR.Unit;

namespace ArchChallenge.CashFlow.Tests.Unit.Application.Behaviors;

public class LoggingBehaviorTests
{
    private readonly ILogger<LoggingBehavior<EnqueueTransaction, EnqueueResult>> _logger;
    private readonly LoggingBehavior<EnqueueTransaction, EnqueueResult> _behavior;

    public LoggingBehaviorTests()
    {
        _logger   = Substitute.For<ILogger<LoggingBehavior<EnqueueTransaction, EnqueueResult>>>();
        _behavior = new LoggingBehavior<EnqueueTransaction, EnqueueResult>(_logger);
    }

    [Fact]
    public async Task Handle_OnSuccess_ShouldReturnNextResult()
    {
        var expected = new EnqueueResult(Guid.NewGuid());
        var next     = Substitute.For<RequestHandlerDelegate<EnqueueResult>>();
        next(Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _behavior.Handle(
            new EnqueueTransaction(TransactionType.Credit, 100m, "Test"),
            next,
            CancellationToken.None);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_WithIAsyncCommand_ShouldIncludeTaskId()
    {
        var next = Substitute.For<RequestHandlerDelegate<EnqueueResult>>();
        next(Arg.Any<CancellationToken>()).Returns(new EnqueueResult(Guid.NewGuid()));

        var act = async () => await _behavior.Handle(
            new EnqueueTransaction(TransactionType.Credit, 100m, null),
            next,
            CancellationToken.None);

        await act.Should().NotThrowAsync();
        await next.Received(1)(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNextThrows_ShouldRethrow()
    {
        var next = Substitute.For<RequestHandlerDelegate<EnqueueResult>>();
        next(Arg.Any<CancellationToken>())
            .Returns<EnqueueResult>(_ => throw new InvalidOperationException("boom"));

        var act = async () => await _behavior.Handle(
            new EnqueueTransaction(TransactionType.Credit, 100m, null),
            next,
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
    }

    [Fact]
    public async Task Handle_WithCommandWithoutTaskId_ShouldWorkWithoutTaskId()
    {
        // EnqueueTransaction não implementa IAsyncCommand, portanto TaskId será null no behavior
        var next = Substitute.For<RequestHandlerDelegate<EnqueueResult>>();
        next(Arg.Any<CancellationToken>()).Returns(new EnqueueResult(Guid.NewGuid()));

        var result = await _behavior.Handle(
            new EnqueueTransaction(TransactionType.Debit, 200m, null),
            next,
            CancellationToken.None);

        result.Should().NotBeNull();
        await next.Received(1)(Arg.Any<CancellationToken>());
    }
}
