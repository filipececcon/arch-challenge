namespace ArchChallenge.CashFlow.Application.Abstractions.Commands;

/// <summary>
/// Marca comandos assíncronos consumidos de uma fila (consumer-side).
/// Estes gerenciam transação e task-cache diretamente no handler.
/// </summary>
public interface IAsyncCommand : IRequest
{
    Guid TaskId { get; }
}
