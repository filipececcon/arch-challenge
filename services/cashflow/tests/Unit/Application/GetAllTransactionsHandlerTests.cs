using ArchChallenge.CashFlow.Application.Transactions.Queries.GetAllTransactions;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using ArchChallenge.CashFlow.Infrastructure.Data.Documents.Models;
using FluentAssertions;
using NSubstitute;
using System.Linq.Expressions;

namespace ArchChallenge.CashFlow.Tests.Unit.Application;

public class GetAllTransactionsHandlerTests
{
    private readonly IDocumentsReadRepository<TransactionDocument> _repository;
    private readonly GetAllTransactionsHandler _handler;

    public GetAllTransactionsHandlerTests()
    {
        _repository = Substitute.For<IDocumentsReadRepository<TransactionDocument>>();
        _handler    = new GetAllTransactionsHandler(_repository);
    }

    [Fact]
    public async Task Handle_WithNoFilters_ShouldReturnAllDocuments()
    {
        var docs = BuildDocuments(3);
        _repository
            .ListAsync(Arg.Any<Expression<Func<TransactionDocument, bool>>?>(),
                       Arg.Any<Expression<Func<TransactionDocument, object>>?>(),
                       Arg.Any<bool>(),
                       Arg.Any<CancellationToken>())
            .Returns(docs);

        var result = await _handler.Handle(new GetAllTransactionsQuery(), CancellationToken.None);

        result.Transactions.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithEmptyRepository_ShouldReturnEmptyList()
    {
        _repository
            .ListAsync(Arg.Any<Expression<Func<TransactionDocument, bool>>?>(),
                       Arg.Any<Expression<Func<TransactionDocument, object>>?>(),
                       Arg.Any<bool>(),
                       Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TransactionDocument>());

        var result = await _handler.Handle(new GetAllTransactionsQuery(), CancellationToken.None);

        result.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldMapDocumentFieldsCorrectly()
    {
        var id  = Guid.NewGuid();
        var doc = new TransactionDocument
        {
            Id          = id,
            Type        = "Credit",
            Amount      = 250m,
            Description = "Test mapping",
            CreatedAt   = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Active      = true
        };

        _repository
            .ListAsync(Arg.Any<Expression<Func<TransactionDocument, bool>>?>(),
                       Arg.Any<Expression<Func<TransactionDocument, object>>?>(),
                       Arg.Any<bool>(),
                       Arg.Any<CancellationToken>())
            .Returns(new[] { doc });

        var result = await _handler.Handle(new GetAllTransactionsQuery(), CancellationToken.None);

        var tx = result.Transactions.Single();
        tx.Id.Should().Be(id);
        tx.Type.Should().Be("Credit");
        tx.Amount.Should().Be(250m);
        tx.Description.Should().Be("Test mapping");
        tx.Active.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithAnyFilter_ShouldCallRepositoryOnce()
    {
        _repository
            .ListAsync(Arg.Any<Expression<Func<TransactionDocument, bool>>?>(),
                       Arg.Any<Expression<Func<TransactionDocument, object>>?>(),
                       Arg.Any<bool>(),
                       Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TransactionDocument>());

        await _handler.Handle(
            new GetAllTransactionsQuery(Type: "Credit", MinAmount: 10m, MaxAmount: 500m),
            CancellationToken.None);

        await _repository.Received(1)
            .ListAsync(
                Arg.Any<Expression<Func<TransactionDocument, bool>>?>(),
                Arg.Any<Expression<Func<TransactionDocument, object>>?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNoFilters_ShouldCallRepositoryOnce()
    {
        _repository
            .ListAsync(Arg.Any<Expression<Func<TransactionDocument, bool>>?>(),
                       Arg.Any<Expression<Func<TransactionDocument, object>>?>(),
                       Arg.Any<bool>(),
                       Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TransactionDocument>());

        await _handler.Handle(new GetAllTransactionsQuery(), CancellationToken.None);

        await _repository.Received(1)
            .ListAsync(
                Arg.Any<Expression<Func<TransactionDocument, bool>>?>(),
                Arg.Any<Expression<Func<TransactionDocument, object>>?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>());
    }

    private static IReadOnlyList<TransactionDocument> BuildDocuments(int count) =>
        Enumerable.Range(1, count).Select(i => new TransactionDocument
        {
            Id     = Guid.NewGuid(),
            Type   = i % 2 == 0 ? "Debit" : "Credit",
            Amount = i * 100m,
            Active = true
        }).ToList();
}
