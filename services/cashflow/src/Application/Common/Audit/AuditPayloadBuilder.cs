using System.Text.Json;
using ArchChallenge.CashFlow.Application.Utils;

namespace ArchChallenge.CashFlow.Application.Common.Audit;

/// <summary>
/// Centraliza a serialização dos payloads de auditoria para cada tipo de agregado.
/// Os handlers escrevem o JSON diretamente no outbox de <see cref="Domain.Shared.Entities.OutboxTarget.Audit"/>,
/// sem depender do <c>AuditContext</c> ou da <c>UnitOfWork</c>.
/// </summary>
public static class AuditPayloadBuilder
{
    /// <summary>
    /// Payload de auditoria para operações na conta corrente.
    /// </summary>
    /// <param name="account">Estado atual do agregado.</param>
    /// <param name="eventName">Nome canônico do evento de domínio.</param>
    /// <param name="userId">UserId do usuário que originou o comando.</param>
    /// <param name="occurredAt">Instante em que o comando foi recebido.</param>
    /// <param name="relatedTransactionId">Transação relacionada (apenas para ExecuteTransaction).</param>
    public static string ForAccount(
        Account  account,
        string   eventName,
        string   userId,
        DateTime occurredAt,
        Guid?    relatedTransactionId = null)
        => JsonSerializer.Serialize(new
        {
            auditId            = Guid.NewGuid().ToString("D"),
            userId,
            occurredAt,
            eventName,
            aggregateType      = nameof(Account),
            aggregateId        = account.Id.ToString("D"),
            state = new
            {
                accountId            = account.Id,
                accountUserId        = account.UserId,
                balance              = account.Balance,
                relatedTransactionId
            }
        }, SerializeUtils.EntityJsonOptions);
}

