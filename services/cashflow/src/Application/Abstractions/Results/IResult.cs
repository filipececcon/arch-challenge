namespace ArchChallenge.CashFlow.Application.Abstractions.Results;

/// <summary>
/// Interface não-genérica para inspecionar resultado de forma polimórfica
/// (usado pelo <c>UnitOfWorkBehavior</c>).
/// </summary>
public interface IResult
{
    bool IsSuccess { get; }
    bool IsFailure { get; }
    int StatusCode { get; }
}
