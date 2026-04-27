using System.Text.Json.Serialization;

namespace ArchChallenge.CashFlow.Application.Abstractions.Results;

/// <summary>
/// Envelope de resposta simplificado. Timestamp e enriquecimento ficam no middleware/action filter.
/// </summary>
public sealed record Result<T> : IResult where T : class
{
    private const int Min2xx = 200;
    private const int Max2xx = 299;
    private const int Min4xx = 400;
    private const int Max4xx = 499;

    [JsonPropertyName("success")]
    public bool IsSuccess { get; init; }

    [JsonIgnore]
    public bool IsFailure => !IsSuccess;

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; init; }

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<string> Errors { get; init; } = [];

    public object? GetData() => Data;

    public static Result<T> Ok(T? data, int statusCode = 200)
    {
        Ensure2xx(statusCode);

        return new Result<T>
        {
            IsSuccess  = true,
            StatusCode = statusCode,
            Data       = data,
            Errors     = []
        };
    }

    public static Result<T> Fail(int statusCode, IReadOnlyList<string> errors)
    {
        Ensure4xx(statusCode);

        if (errors is null || errors.Count is 0)
            throw new ArgumentException(@"Failure requires at least one error message.", nameof(errors));

        return new Result<T>
        {
            IsSuccess  = false,
            StatusCode = statusCode,
            Errors     = [.. errors]
        };
    }

    public static Result<T> Fail(int statusCode, params string[] errors) =>
        Fail(statusCode, (IReadOnlyList<string>)errors);

    public static Result<T> NotFound(string? message = null) =>
        Fail(404, message is null or "" ? "Not found" : message);

    private static void Ensure2xx(int statusCode)
    {
        if (statusCode is >= Min2xx and <= Max2xx) return;
        throw new ArgumentOutOfRangeException(nameof(statusCode), statusCode,
            $@"The success status should be in the HTTP 2xx range ({Min2xx}-{Max2xx}).");
    }

    private static void Ensure4xx(int statusCode)
    {
        if (statusCode is >= Min4xx and <= Max4xx) return;
        throw new ArgumentOutOfRangeException(nameof(statusCode), statusCode,
            $@"Client failure status must be in the HTTP 4xx range ({Min4xx}-{Max4xx}).");
    }
}
