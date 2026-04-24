using ArchChallenge.CashFlow.Application.Abstractions.Results;

namespace ArchChallenge.CashFlow.Application.Abstractions.Responses;

/// <summary>
/// Marker para respostas de requests — o <c>ResultBehavior</c> intercepta e transforma automaticamente
/// qualquer resposta de request que implemente esta interface em um <see cref="Result{TResponse}"/>,
/// encapsulando o valor de resposta ou a falha, sem necessidade de tratamento manual pelo handler.
/// Assim, handlers podem retornar diretamente o valor de resposta esperado, e o comportamento se encarega
/// de envolver em um resultado, simplificando a implementação e mantendo a consistência na forma como os
/// resultados são tratados em toda a aplicação.
/// </summary>
public interface IResponse
{
    
}
