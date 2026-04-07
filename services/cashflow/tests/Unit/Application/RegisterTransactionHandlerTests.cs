using ArchChallenge.CashFlow.Application.Common.Notifications;
using ArchChallenge.CashFlow.Application.Transactions.Commands.RegisterTransaction;
using ArchChallenge.CashFlow.Domain.Entities;
using ArchChallenge.CashFlow.Domain.Enums;
using ArchChallenge.CashFlow.Domain.Events;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using FluentAssertions;
using MediatR;
using NSubstitute;

namespace ArchChallenge.CashFlow.Tests.Unit.Application;

public class RegisterTransactionHandlerTests
{
    private readonly IWriteRepository<Transaction> _repository;
    private readonly IPublisher _publisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransaction _transaction;
    private readonly RegisterTransactionHandler _handler;

    public RegisterTransactionHandlerTests()
    {
        _repository = Substitute.For<IWriteRepository<Transaction>>();
        _publisher = Substitute.For<IPublisher>();
        _transaction = Substitute.For<ITransaction>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(_transaction);
        _handler = new RegisterTransactionHandler(_repository, _publisher, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldCommitAndPublishEvent()
    {
        var command = new RegisterTransactionCommand(
            TransactionType.Credit,
            150.00m,
            "Cash sale");

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Type.Should().Be(TransactionType.Credit);
        result.Amount.Should().Be(150.00m);

        await _repository.Received(1).AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(
            Arg.Is<INotification>(n => n is DomainEventNotification<TransactionRegisteredEvent>),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectData()
    {
        var command = new RegisterTransactionCommand(TransactionType.Debit, 300m, "Stock purchase");

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.Type.Should().Be(TransactionType.Debit);
        result.Amount.Should().Be(300m);
        result.Description.Should().Be("Stock purchase");
    }

    [Fact]
    public async Task Handle_WithInvalidAmount_ShouldNotPersistCommitOrPublish()
    {
        var command = new RegisterTransactionCommand(TransactionType.Debit, -50m, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Notifications.Should().ContainSingle(e => e.Key == "Amount");

        await _repository.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_ShouldRollbackAndNotPublish()
    {
        var command = new RegisterTransactionCommand(TransactionType.Credit, 100m, "Test");

        _repository
            .AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("DB error")));

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }
}
