using ArchChallenge.CashFlow.Domain.Enums;

namespace ArchChallenge.CashFlow.Application.Transactions.Enqueue;

/// <summary>
/// Mensagem de evento publicada para a fila de transações, contendo os dados necessários para o processamento da transação.
/// Esta mensagem é consumida por um handler específico que irá realizar a transação financeira.
/// </summary>
/// <param name="TaskId"></param>
/// <param name="UserId"></param>
/// <param name="OccurredAt"></param>
/// <param name="Type"></param>
/// <param name="Amount"></param>
/// <param name="Description"></param>
public record EnqueueTransactionMessage(
    Guid TaskId,
    string UserId,
    DateTime OccurredAt,
    TransactionType Type,
    decimal Amount,
    string? Description);
