using ArchChallenge.CashFlow.Application.Common.Enqueue;
using ArchChallenge.CashFlow.Application.Common.Tasks;
using ArchChallenge.CashFlow.Application.Common.Interfaces;
using ArchChallenge.CashFlow.Application.Transactions.Commands.EnqueueTransaction;
using ArchChallenge.CashFlow.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace ArchChallenge.CashFlow.Tests.Unit.Application;

public class EnqueueCommandHandlerTests
{
    private readonly ITaskCacheService _taskCache;
    private readonly IEventBus _eventBus;
    private readonly EnqueueCommandHandler<EnqueueTransaction, EnqueueTransactionMessage> _handler;

    public EnqueueCommandHandlerTests()
    {
        _taskCache = Substitute.For<ITaskCacheService>();
        _eventBus  = Substitute.For<IEventBus>();
        _handler   = new EnqueueCommandHandler<EnqueueTransaction, EnqueueTransactionMessage>(_taskCache, _eventBus);
    }

    [Fact]
    public async Task Handle_WithoutIdempotencyKey_ShouldSetPendingAndPublishAndReturnTaskId()
    {
        var command = BuildCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.TaskId.Should().NotBeEmpty();
        await _taskCache.Received(1).SetPendingAsync(result.TaskId, Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(Arg.Any<EnqueueTransactionMessage>(), Arg.Any<CancellationToken>());
        await _taskCache.DidNotReceive().SetIdempotencyAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithIdempotencyKey_WhenKeyNotExists_ShouldSetIdempotencyAndPublish()
    {
        var key     = Guid.NewGuid();
        var command = BuildCommand(idempotencyKey: key);

        _taskCache.GetIdempotencyAsync(key, Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.TaskId.Should().NotBeEmpty();
        await _taskCache.Received(1).SetPendingAsync(result.TaskId, Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(Arg.Any<EnqueueTransactionMessage>(), Arg.Any<CancellationToken>());
        await _taskCache.Received(1).SetIdempotencyAsync(key, result.TaskId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithIdempotencyKey_WhenKeyAlreadyExists_ShouldReturnExistingTaskIdWithoutPublishing()
    {
        var key            = Guid.NewGuid();
        var existingTaskId = Guid.NewGuid();
        var command        = BuildCommand(idempotencyKey: key);

        _taskCache.GetIdempotencyAsync(key, Arg.Any<CancellationToken>()).Returns(existingTaskId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.TaskId.Should().Be(existingTaskId);
        await _taskCache.DidNotReceive().SetPendingAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
        await _taskCache.DidNotReceive().SetIdempotencyAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldBuildMessageWithCorrectTaskId()
    {
        var command = BuildCommand();

        EnqueueTransactionMessage? captured = null;
        await _eventBus.PublishAsync(
            Arg.Do<EnqueueTransactionMessage>(m => captured = m),
            Arg.Any<CancellationToken>());

        var result = await _handler.Handle(command, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.TaskId.Should().Be(result.TaskId);
        captured.Type.Should().Be(TransactionType.Credit);
        captured.Amount.Should().Be(150m);
    }

    private static EnqueueTransaction BuildCommand(Guid? idempotencyKey = null)
        => new EnqueueTransaction(TransactionType.Credit, 150m, "Test") { IdempotencyKey = idempotencyKey };
}
