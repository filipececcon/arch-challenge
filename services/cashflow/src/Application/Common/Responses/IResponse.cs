namespace ArchChallenge.CashFlow.Application.Common.Responses;

/// <summary>
/// Marca DTOs de saída (comandos e consultas) que podem compor o payload de
/// <see cref="Result{T}"/>. Metadados de trânsito (HTTP, sucesso/erro, carimbo, erros) ficam
/// no envelope; o payload concentra só dados de negócio.
/// </summary>
public interface IResponse
{
    
}
