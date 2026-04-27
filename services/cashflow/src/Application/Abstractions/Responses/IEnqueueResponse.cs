namespace ArchChallenge.CashFlow.Application.Abstractions.Responses;

/// <summary>
/// Contrato para respostas de comandos de enfileiramento.
/// Garante em tempo de compilação que todo resultado de um <c>EnqueueCommand</c>
/// expõe o <c>TaskId</c> gerado pelo <c>EnqueueTaskCacheBehavior</c>.
/// </summary>
public interface IEnqueueResponse : IResponse
{
    Guid TaskId { get; }
}
