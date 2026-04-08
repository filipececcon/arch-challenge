using System.Text.Json.Serialization;

namespace ArchChallenge.CashFlow.Application.Common.Tasks;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskStatus
{
    Pending,
    Success,
    Failure
}
