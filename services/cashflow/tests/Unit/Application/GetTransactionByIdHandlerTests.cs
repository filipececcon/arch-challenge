using ArchChallenge.CashFlow.Application.Transactions.Events.TransactionProcessed;
using ArchChallenge.CashFlow.Application.Transactions.Queries.GetTransactionById;
using ArchChallenge.CashFlow.Domain.Entities;
using ArchChallenge.CashFlow.Domain.Enums;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using ArchChallenge.CashFlow.Domain.Shared.Specifications;
using ArchChallenge.CashFlow.Infrastructure.Data.Documents.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ArchChallenge.CashFlow.Tests.Unit.Application;

public class GetTransactionByIdHandlerTests
{
    private readonly IDocumentsReadRepository<TransactionDocument> _documentsRepository;
    private readonly IReadRepository<Transaction>                  _relationalRepository;
    private readonly IOutboxRepository                             _outboxRepository;
    private readonly ILogger<GetTransactionByIdHandler>            _logger;
    private readonly GetTransactionByIdHandler                     _handler;

    public GetTransactionByIdHandlerTests()
    {
        _documentsRepository  = Substitute.For<IDocumentsReadRepository<TransactionDocument>>();
        _relationalRepository = Substitute.For<IReadRepository<Transaction>>();
        _outboxRepository     = Substitute.For<IOutboxRepository>();
        _logger               = Substitute.For<ILogger<GetTransactionByIdHandler>>();

        _handler = new GetTransactionByIdHandler(
            _documentsRepository,
            _relationalRepository,
            _outboxRepository,
            _logger);
    }

    [Fact]
    public async Task Handle_WhenDocumentExistsInMongo_ShouldReturnResultWithoutHittingRelational()
    {
        var id       = Guid.NewGuid();
        var document = BuildDocument(id);
        var query    = new GetTransactionByIdQuery(id);

        _documentsRepository.FindOneByIdAsync(id, Arg.Any<CancellationToken>()).Returns(document);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Type.Should().Be("Credit");
        result.Amount.Should().Be(100m);
        result.Active.Should().BeTrue();

        await _outboxRepository.DidNotReceive()
            .HasPendingForAggregateAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _relationalRepository.DidNotReceive()
            .FirstOrDefaultAsync(Arg.Any<ISpecification<Transaction>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMongoEmpty_AndNoPendingOutbox_ShouldReturnNull()
    {
        var id    = Guid.NewGuid();
        var query = new GetTransactionByIdQuery(id);

        _documentsRepository.FindOneByIdAsync(id, Arg.Any<CancellationToken>()).Returns((TransactionDocument?)null);
        _outboxRepository
            .HasPendingForAggregateAsync(TransactionProcessedMessage.EventName, id, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
        await _relationalRepository.DidNotReceive()
            .FirstOrDefaultAsync(Arg.Any<ISpecification<Transaction>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMongoEmpty_AndPendingOutbox_AndEntityInRelational_ShouldReturnResultFromRelational()
    {
        var id         = Guid.NewGuid();
        var entity     = new Transaction(TransactionType.Debit, 250m, "Supplier payment");
        var query      = new GetTransactionByIdQuery(id);

        _documentsRepository.FindOneByIdAsync(id, Arg.Any<CancellationToken>()).Returns((TransactionDocument?)null);
        _outboxRepository
            .HasPendingForAggregateAsync(TransactionProcessedMessage.EventName, id, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _relationalRepository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<Transaction>>(), Arg.Any<CancellationToken>())
            .Returns(entity);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Amount.Should().Be(250m);
        result.Type.Should().Be("Debit");
    }

    [Fact]
    public async Task Handle_WhenMongoEmpty_AndPendingOutbox_ButEntityMissingInRelational_ShouldReturnNull()
    {
        var id    = Guid.NewGuid();
        var query = new GetTransactionByIdQuery(id);

        _documentsRepository.FindOneByIdAsync(id, Arg.Any<CancellationToken>()).Returns((TransactionDocument?)null);
        _outboxRepository
            .HasPendingForAggregateAsync(TransactionProcessedMessage.EventName, id, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _relationalRepository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<Transaction>>(), Arg.Any<CancellationToken>())
            .Returns((Transaction?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    private static TransactionDocument BuildDocument(Guid id) => new()
    {
        Id          = id,
        Type        = "Credit",
        Amount      = 100m,
        Description = "Test",
        CreatedAt   = DateTime.UtcNow,
        UpdatedAt   = DateTime.UtcNow,
        Active      = true
    };
}
