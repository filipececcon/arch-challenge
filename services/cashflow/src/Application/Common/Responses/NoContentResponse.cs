namespace ArchChallenge.CashFlow.Application.Common.Responses;

/// <summary>Payload mínimo para respostas sem corpo (ex.: 204 com envelope JSON).</summary>
public sealed record NoContentResponse : IResponse
{
    public static NoContentResponse Value { get; } = new();

    private NoContentResponse() { }
}
