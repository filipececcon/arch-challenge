using ArchChallenge.CashFlow.Application.Abstractions.Responses;

namespace ArchChallenge.CashFlow.Application.Accounts.Activate;

public record ActivateAccountCommand : SyncCommand<NoContentResponse>;