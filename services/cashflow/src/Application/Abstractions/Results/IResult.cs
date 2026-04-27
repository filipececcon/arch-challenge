namespace ArchChallenge.CashFlow.Application.Abstractions.Results;

/// <summary>
/// Interface não-genérica para inspecionar resultado de forma polimórfica
/// (usado pelos behaviors <c>UnitOfWorkBehavior</c> e <c>TaskCacheBehavior</c>).
/// </summary>
public interface IResult
{
    bool IsSuccess { get; }
    bool IsFailure { get; }
    int StatusCode { get; }
    IReadOnlyList<string> Errors { get; }

    /// <summary>Retorna o dado encapsulado (usado pelo <c>TaskCacheBehavior</c> para serializar o payload de sucesso).</summary>
    object? GetData();
}
