namespace ArchChallenge.CashFlow.Application.Common.Responses;

/// <summary>Acumula mensagens; no fim <see cref="BuildFailure{T}"/> com status 4xx.</summary>
public sealed class ResultBuilder
{
    private readonly List<string> _errors = [];
    
    public bool HasErrors => _errors.Count > 0;

    /// <summary>Mensagens acumuladas (leitura para montar <see cref="Result{T}"/> ou escolher HTTP).</summary>
    public IReadOnlyList<string> Messages => _errors;

    public void AddError(string message)
    {
        _errors.Add(message);
    }

    public void AddErrors(string[] messages)
    {
        _errors.AddRange(messages);
    }
    
    public Result<T> BuildFailure<T>(int statusCode, string? appCode = null, T? data = null) where T : class, IResponse =>
        Result<T>.Fail(statusCode, _errors, appCode, data);
}