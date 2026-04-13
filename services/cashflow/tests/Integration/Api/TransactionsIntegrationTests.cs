using System.Net;
using System.Net.Http.Json;
using ArchChallenge.CashFlow.Api;
using ArchChallenge.CashFlow.Application.Transactions.Commands.EnqueueTransaction;
using ArchChallenge.CashFlow.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ArchChallenge.CashFlow.Tests.Integration.Api;

public class TransactionsIntegrationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task POST_Transactions_ShouldReturn202WithTaskId()
    {
        var command = new EnqueueTransactionCommand(
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
