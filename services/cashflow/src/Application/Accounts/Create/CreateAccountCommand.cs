using ArchChallenge.CashFlow.Application.Abstractions.Commands;

namespace ArchChallenge.CashFlow.Application.Accounts.Create;

public record CreateAccountCommand : IAuditable, ICommand<CreateAccountResult>
{
    public string UserId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
