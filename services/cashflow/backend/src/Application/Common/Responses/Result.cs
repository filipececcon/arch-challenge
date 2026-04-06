using Flunt.Notifications;

namespace ArchChallenge.CashFlow.Application.Common.Responses;

public abstract class Result : Notifiable<Notification>
{
    public bool IsFailure => !IsValid;
}