using System.Text.Json.Serialization;

namespace ArchChallenge.CashFlow.Application.Common.Responses;

/// <summary>
/// Envelope de resposta. Sucesso com <see cref="Ok"/> (status 2xx); falha com estáticos <c>Fail</c> (status 4xx).
/// </summary>
public sealed record Result<T> where T : class, IResponse
{
    private const int Min2xx = 200;
    private const int Max2xx = 299;
    private const int Min4xx = 400;
    private const int Max4xx = 499;

    [JsonPropertyName("success")]
    [JsonPropertyOrder(0)]
    public bool IsSuccess { get; init; }

    [JsonIgnore]
    public bool IsFailure => !IsSuccess;

    [JsonPropertyName("statusCode")]
    [JsonPropertyOrder(2)]
    public int StatusCode { get; init; }

    [JsonPropertyName("code")]
    [JsonPropertyOrder(3)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AppCode { get; init; }

    [JsonPropertyName("timestamp")]
    [JsonPropertyOrder(4)]
    public DateTime UtcTimestamp { get; init; }

    [JsonPropertyName("data")]
    [JsonPropertyOrder(5)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; init; }

    [JsonPropertyName("errors")]
    [JsonPropertyOrder(6)]
    public IReadOnlyList<string> Errors { get; init; } = [];
    
    public static ResultBuilder Builder() => new();

    /// <summary>Resposta de sucesso; <paramref name="statusCode"/> deve ser 2xx.</summary>
    public static Result<T> Ok(T? data, int statusCode = 200, string? appCode = null)
    {
        Ensure2xx(statusCode);

        return new Result<T>
        {
            IsSuccess    = true,
            StatusCode   = statusCode,
            AppCode      = appCode,
            UtcTimestamp = DateTime.UtcNow,
            Data         = data,
            Errors       = []
        };
    }

    /// <summary>Falha de cliente/negócio; <paramref name="statusCode"/> deve ser 4xx. Requer ao menos uma mensagem.</summary>
    public static Result<T> Fail(int statusCode, IReadOnlyList<string> errors, string? appCode = null, T? data = null)
    {
        Ensure4xx(statusCode);

        if (errors is null || errors.Count is 0)
            throw new ArgumentException(@"Failure requires at least one error message..", nameof(errors));

        return new Result<T>
        {
            IsSuccess    = false,
            StatusCode   = statusCode,
            AppCode      = appCode,
            UtcTimestamp = DateTime.UtcNow,
            Data         = data,
            Errors       = [.. errors]
        };
    }

    public static Result<T> Fail(int statusCode, string error, string? appCode = null, T? data = null) =>
        Fail(statusCode, [error], appCode, data);

    public static Result<T> NotFound(string? message = null) =>
        Fail(404, message is null or "" ? ["Not found"] : [message], "not_found");

    private static void Ensure2xx(int statusCode)
    {
        if (statusCode is >= Min2xx and <= Max2xx) return;
        
        var msg = $@"The success status should be in the HTTP 2xx range ({Min2xx}-{Max2xx}).";
            
        throw new ArgumentOutOfRangeException(nameof(statusCode), statusCode, msg);
    }

    private static void Ensure4xx(int statusCode)
    {
        if (statusCode is >= Min4xx and <= Max4xx) return;
        
        var msg = $@"Client failure status must be in the HTTP 4xx range ({Min4xx}-{Max4xx})."; 
            
        throw new ArgumentOutOfRangeException(nameof(statusCode), statusCode, msg);
    }
}
