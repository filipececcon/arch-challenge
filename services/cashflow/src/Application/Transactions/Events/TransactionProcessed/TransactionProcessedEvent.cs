namespace ArchChallenge.CashFlow.Application.Transactions.Events.TransactionProcessed;

/// <summary>
/// Notificação de aplicação disparada após o processamento bem-sucedido de uma transação.
///
/// Utilizada internamente via MediatR para desacoplar o caso de uso das ações
/// secundárias (ex: publicação no broker), sem depender de infraestrutura de
/// eventos de domínio.
/// </summary>
public sealed record TransactionProcessedEvent(TransactionProcessedMessage Payload) : INotification;
