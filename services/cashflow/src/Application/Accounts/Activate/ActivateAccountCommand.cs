using ArchChallenge.CashFlow.Application.Abstractions.Commands;
using ArchChallenge.CashFlow.Application.Abstractions.Responses;

namespace ArchChallenge.CashFlow.Application.Accounts.Activate;

public record ActivateAccountCommand : IAuditable, ICommand<NoContentResponse>
{
    public string UserId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
