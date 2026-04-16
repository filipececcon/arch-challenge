using System.Text.Json;
using ArchChallenge.CashFlow.Domain.Shared.Projection;
using FluentAssertions;

namespace ArchChallenge.CashFlow.Tests.Unit.Domain;

public class EntityProjectionJsonTests
{
    [Fact]
    public void RemoveRuntimeFields_ShouldRemoveFluntAndEntityBaseProperties()
    {
        var json = """
            {
                "id": "abc",
                "amount": 100,
                "Notifications": [],
                "IsValid": true,
                "Active": true,
                "CreatedAt": "2026-01-01T00:00:00Z",
                "UpdatedAt": "2026-01-01T00:00:00Z"
            }
            """;

        var element = JsonSerializer.Deserialize<JsonElement>(json);

        var result = EntityProjectionJson.RemoveRuntimeFields(element);

        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(result.GetRawText())!;

        dict.Should().NotContainKey("Notifications");
        dict.Should().NotContainKey("isValid");
        dict.Should().ContainKey("id");
        dict.Should().ContainKey("amount");
    }

    [Fact]
    public void RemoveRuntimeFields_ShouldConvertKeysToCamelCase()
    {
        var json = """{"TransactionType": "Credit", "TotalAmount": 999}""";

        var element = JsonSerializer.Deserialize<JsonElement>(json);

        var result = EntityProjectionJson.RemoveRuntimeFields(element);

        var raw = result.GetRawText();
        raw.Should().Contain("transactionType");
        raw.Should().Contain("totalAmount");
        raw.Should().NotContain("TransactionType");
        raw.Should().NotContain("TotalAmount");
    }

    [Fact]
    public void RemoveRuntimeFields_WithArrayElement_ShouldReturnElementNormalized()
    {
        var json    = """[{"Name": "A"}, {"Name": "B"}]""";
        var element = JsonSerializer.Deserialize<JsonElement>(json);

        var result = EntityProjectionJson.RemoveRuntimeFields(element);

        result.ValueKind.Should().Be(JsonValueKind.Array);
        var items = result.EnumerateArray().ToList();
        items.Should().HaveCount(2);
        items[0].GetRawText().Should().Contain("name");
    }

    [Fact]
    public void RemoveRuntimeFields_WithPrimitiveElement_ShouldReturnAsIs()
    {
        var element = JsonSerializer.Deserialize<JsonElement>("42");

        var result = EntityProjectionJson.RemoveRuntimeFields(element);

        result.GetInt32().Should().Be(42);
    }

    [Fact]
    public void RemoveRuntimeFields_WithNestedObject_ShouldConvertAllKeysToCamelCase()
    {
        var json = """
            {
                "Transaction": {
                    "TransactionType": "Credit",
                    "Amount": 100
                }
            }
            """;

        var element = JsonSerializer.Deserialize<JsonElement>(json);

        var result = EntityProjectionJson.RemoveRuntimeFields(element);
        var raw    = result.GetRawText();

        raw.Should().Contain("transaction");
        raw.Should().Contain("transactionType");
        raw.Should().NotContain("Transaction\":");
    }
}
