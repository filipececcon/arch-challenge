namespace ArchChallenge.CashFlow.Domain.Shared.Notifications;

public sealed record Notification(string Field, string Message)
{
    public static Notification Create(string field, string message) => new(field, message);
}
