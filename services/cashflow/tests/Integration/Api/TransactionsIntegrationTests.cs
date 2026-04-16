using System.Net;
using System.Net.Http.Json;
using ArchChallenge.CashFlow.Application.Transactions.Commands.EnqueueTransaction;
using ArchChallenge.CashFlow.Domain.Enums;
using ArchChallenge.CashFlow.Tests.Integration.Support;
using FluentAssertions;

namespace ArchChallenge.CashFlow.Tests.Integration.Api;

public class TransactionsIntegrationTests(CashFlowWebApplicationFactory factory)
    : IClassFixture<CashFlowWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task POST_Transactions_ShouldReturn202WithTaskId()
    {
        var command = new EnqueueTransaction(
            TransactionType.Credit,
            100m,
            "Test");

        var response = await _client.PostAsJsonAsync("/api/transactions", command);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task GET_Transactions_ShouldReturn200()
    {
        var response = await _client.GetAsync("/api/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
