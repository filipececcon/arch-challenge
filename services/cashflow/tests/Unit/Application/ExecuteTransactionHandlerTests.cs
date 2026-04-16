using System.Text.Json;
using ArchChallenge.CashFlow.Application.Common.Interfaces;
using ArchChallenge.CashFlow.Application.Common.Tasks;
using ArchChallenge.CashFlow.Application.Transactions.Commands.ExecuteTransaction;
using ArchChallenge.CashFlow.Application.Transactions.Events.TransactionProcessed;
using ArchChallenge.CashFlow.Domain.Entities;
using ArchChallenge.CashFlow.Domain.Enums;
using ArchChallenge.CashFlow.Domain.Shared.Audit;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.I18n;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using NSubstitute;

namespace ArchChallenge.CashFlow.Tests.Unit.Application;

public class ExecuteTransactionHandlerTests
{
    private readonly IWriteRepository<Transaction> _repository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IEventBus _eventBus;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDbTransaction _dbTransaction;
    private readonly ITaskCacheService _taskCache;
    private readonly IStringLocalizer<Messages> _localizer;
    private readonly IAuditContext _auditContext;
    private readonly ExecuteTransactionHandler _handler;

    public ExecuteTransactionHandlerTests()
    {
        _repository       = Substitute.For<IWriteRepository<Transaction>>();
        _outboxRepository = Substitute.For<IOutboxRepository>();
        _eventBus         = Substitute.For<IEventBus>();
        _dbTransaction    = Substitute.For<IDbTransaction>();
        _taskCache        = Substitute.For<ITaskCacheService>();
        _unitOfWork       = Substitute.For<IUnitOfWork>();
        _localizer        = Substitute.For<IStringLocalizer<Messages>>();
        _auditContext     = Substitute.For<IAuditContext>();
        _localizer[Arg.Any<string>()].Returns(x => new LocalizedString((string)x[0], (string)x[0]));
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(_dbTransaction);
        _handler = new ExecuteTransactionHandler(
            _repository, _outboxRepository, _unitOfWork, _auditContext, _taskCache, _eventBus, _localizer);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldCommitAndMarkCacheAsSuccess()
    {
        var taskId  = Guid.NewGuid();
        var command = new ExecuteTransaction(taskId, TransactionType.Credit, 150m, "Cash sale");

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        await _handler.Handle(command, CancellationToken.None);

        await _repository.Received(1).AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _dbTransaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _dbTransaction.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        await _taskCache.Received(1).SetSuccessAsync(taskId, Arg.Any<JsonElement>(), Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(Arg.Any<TransactionProcessedMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidAmount_ShouldMarkCacheAsFailureAndRollback()
    {
        var taskId  = Guid.NewGuid();
        var command = new ExecuteTransaction(taskId, TransactionType.Debit, -50m, null);

        await _handler.Handle(command, CancellationToken.None);

        await _repository.Received(1).AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
        await _dbTransaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _dbTransaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
        await _taskCache.Received(1).SetFailureAsync(taskId, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_ShouldRollbackAndMarkCacheAsFailure()
    {
        var taskId  = Guid.NewGuid();
        var command = new ExecuteTransaction(taskId, TransactionType.Credit, 100m, "Test");

        _repository
            .AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("DB error")));

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        await _dbTransaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _dbTransaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
        await _taskCache.Received(1).SetFailureAsync(taskId, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
