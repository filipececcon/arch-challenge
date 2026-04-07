using System.Net;
using System.Net.Http.Json;
using ArchChallenge.CashFlow.Application.Transactions.Commands.RegisterTransaction;
using ArchChallenge.CashFlow.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ArchChallenge.CashFlow.Tests.Integration.Api;

public class TransactionsIntegrationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task POST_Transactions_ShouldReturn201()
    {
        var command = new RegisterTransactionCommand(
            TransactionType.Credit,
            100m,
            "Test");

        var response = await _client.PostAsJsonAsync("/api/transactions", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GET_Transactions_ShouldReturn200()
    {
        var response = await _client.GetAsync("/api/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
