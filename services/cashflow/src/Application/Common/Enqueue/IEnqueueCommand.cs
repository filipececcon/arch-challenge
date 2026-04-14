namespace ArchChallenge.CashFlow.Application.Common.Enqueue;

/// <summary>
/// Contrato que um command deve implementar para participar do fluxo EDA de enqueue.
/// O handler genérico <see cref="EnqueueCommandHandler{TCommand,TMessage}"/>
/// processa qualquer command que implemente esta interface, sem código adicional.
///
/// Quando <see cref="IdempotencyKey"/> é fornecido, o handler verifica se já existe
/// um taskId associado e retorna o resultado anterior sem reprocessar.
/// </summary>
public interface IEnqueueCommand<out TMessage> : IRequest<EnqueueResult> where TMessage : class
{
    Guid? IdempotencyKey { get; }
    TMessage BuildMessage(Guid taskId);
}
