using System.Text.Json;
using ArchChallenge.CashFlow.Application.Abstractions.Utils;

namespace ArchChallenge.CashFlow.Application.Accounts.Audit;

public static class AccountAuditBuilder
{
    public static string ForAccount(
        Account account,
        string eventName,
        string userId,
        DateTime occurredAt,
        Guid? relatedTransactionId = null)
    {
        return JsonSerializer.Serialize(new
        {
            auditId           = Guid.NewGuid().ToString("D"),
            userId,
            occurredAt,
            eventName,
            aggregateType     = nameof(Account),
            aggregateId       = account.Id.ToString("D"),
            state = new
            {
                accountId            = account.Id,
                accountUserId        = account.UserId,
                balance              = account.Balance,
                relatedTransactionId
            }
        }, SerializeUtils.EntityJsonOptions);
    }
}
