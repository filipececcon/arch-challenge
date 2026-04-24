namespace ArchChallenge.CashFlow.Application.Abstractions.Responses;

public sealed record NoContentResponse : IResponse
{
    public static NoContentResponse Value { get; } = new();
    private NoContentResponse() { }
}
