using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ArchChallenge.CashFlow.Application.Transactions.Commands.EnqueueTransaction;
using ArchChallenge.CashFlow.Domain.Enums;
using ArchChallenge.CashFlow.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ArchChallenge.CashFlow.Tests.Integration.Api;

public class TransactionsIntegrationTests(CashFlowWebApplicationFactory factory)
    : IClassFixture<CashFlowWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // ── POST /api/transactions ──────────────────────────────────────────────

    [Fact]
    public async Task POST_Transactions_ShouldReturn202WithTaskId()
    {
        var command = new EnqueueTransaction(TransactionType.Credit, 100m, "Test");

        var response = await _client.PostAsJsonAsync("/api/transactions", command);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("taskId", out var taskIdProp).Should().BeTrue("a resposta deve conter taskId");
        taskIdProp.GetGuid().Should().NotBeEmpty();
    }

    [Fact]
    public async Task POST_Transactions_WithZeroAmount_ShouldReturn400WithValidationError()
    {
        var command = new EnqueueTransaction(TransactionType.Credit, 0m, null);

        var response = await _client.PostAsJsonAsync("/api/transactions", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("type", out var typeProp).Should().BeTrue();
        typeProp.GetString().Should().Be("ValidationError");
        body.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task POST_Transactions_WithNegativeAmount_ShouldReturn400()
    {
        var command = new EnqueueTransaction(TransactionType.Debit, -50m, null);

        var response = await _client.PostAsJsonAsync("/api/transactions", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Transactions_WithDescriptionExceeding255Chars_ShouldReturn400()
    {
        var command = new EnqueueTransaction(TransactionType.Credit, 10m, new string('x', 256));

        var response = await _client.PostAsJsonAsync("/api/transactions", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Transactions_WithIdempotencyKey_ShouldReturnSameTaskId()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var command        = new EnqueueTransaction(TransactionType.Credit, 50m, "Idempotency test");

        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);

        var first  = await _client.PostAsJsonAsync("/api/transactions", command);
        var second = await _client.PostAsJsonAsync("/api/transactions", command);

        _client.DefaultRequestHeaders.Remove("Idempotency-Key");

        first.StatusCode.Should().Be(HttpStatusCode.Accepted);
        second.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var firstTaskId  = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("taskId").GetGuid();
        var secondTaskId = (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("taskId").GetGuid();

        firstTaskId.Should().Be(secondTaskId, "idempotência deve retornar o mesmo taskId");
    }

    [Fact]
    public async Task POST_Transactions_WithoutAuth_ShouldReturn401()
    {
        using var unauthenticatedClient = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                HandleCookies     = false,
                AllowAutoRedirect = false
            });

        var command = new EnqueueTransaction(TransactionType.Credit, 100m, "No auth");

        var response = await unauthenticatedClient.PostAsJsonAsync("/api/transactions", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /api/transactions ───────────────────────────────────────────────

    [Fact]
    public async Task GET_Transactions_ShouldReturn200WithTransactionsArray()
    {
        var response = await _client.GetAsync("/api/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("transactions", out _).Should().BeTrue("corpo deve conter a lista 'transactions'");
    }

    [Fact]
    public async Task GET_Transactions_WithInvalidType_ShouldReturn400()
    {
        var response = await _client.GetAsync("/api/transactions?type=INVALID");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_Transactions_WithMinAmountGreaterThanMaxAmount_ShouldReturn400()
    {
        var response = await _client.GetAsync("/api/transactions?minAmount=500&maxAmount=100");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /api/transactions/{id} ──────────────────────────────────────────

    [Fact]
    public async Task GET_TransactionById_WithUnknownId_ShouldReturn404()
    {
        var unknownId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/transactions/{unknownId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
