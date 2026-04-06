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
    private readonly RegisterTransactionHandler _handler;

    public RegisterTransactionHandlerTests()
    {
        _repository = Substitute.For<IWriteRepository<Transaction>>();
        _publisher = Substitute.For<IPublisher>();
        _handler = new RegisterTransactionHandler(_repository, _publisher);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldReturnSuccessAndPublishEvent()
    {
        var command = new RegisterTransactionCommand(
            TransactionType.Credit,
            150.00m,
            "Cash sale");

        _repository.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().NotBeEmpty();
        result.Value.Type.Should().Be(TransactionType.Credit);
        result.Value.Amount.Should().Be(150.00m);

        await _repository.Received(1).AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(
            Arg.Is<INotification>(n => n is DomainEventNotification<TransactionRegisteredEvent>),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectData()
    {
        var command = new RegisterTransactionCommand(TransactionType.Debit, 300m, "Stock purchase");

        _repository.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Type.Should().Be(TransactionType.Debit);
        result.Value.Amount.Should().Be(300m);
        result.Value.Description.Should().Be("Stock purchase");
    }

    [Fact]
    public async Task Handle_WithInvalidAmount_ShouldReturnFailureWithoutPersisting()
    {
        var command = new RegisterTransactionCommand(TransactionType.Debit, -50m, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Field == "Amount");

        await _repository.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }
}
