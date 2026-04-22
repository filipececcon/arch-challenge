using ArchChallenge.CashFlow.Domain.Enums;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.ExecuteTransaction;

/// <summary>
/// Comando para execução de uma transação: criação de entidade <c>Transaction</c> e atualização do saldo da conta.
/// </summary>
/// <param name="TaskId"> </param>
/// <param name="Type"></param>
/// <param name="Amount"></param>
/// <param name="Description"></param>
public record ExecuteTransactionCommand(
    Guid TaskId, 
    TransactionType Type, 
    decimal Amount, 
    string? Description
) : CommandBase, IRequest, IAsyncCommand;
