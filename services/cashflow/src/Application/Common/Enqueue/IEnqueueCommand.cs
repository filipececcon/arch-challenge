namespace ArchChallenge.CashFlow.Application.Common.Enqueue;

/// <summary>
/// Contrato que um command deve implementar para participar do fluxo EDA de enqueue.
/// O handler genérico <see cref="EnqueueCommandHandler{TCommand,TMessage}"/>
/// processa qualquer command que implemente esta interface, sem código adicional.
/// </summary>
public interface IEnqueueCommand<out TMessage> : IRequest<EnqueueResult> where TMessage : class
{
    TMessage BuildMessage(Guid taskId);
}
