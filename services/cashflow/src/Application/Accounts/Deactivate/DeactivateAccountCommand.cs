using ArchChallenge.CashFlow.Application.Abstractions.Responses;

namespace ArchChallenge.CashFlow.Application.Accounts.Deactivate;

public record DeactivateAccountCommand : SyncCommand<NoContentResponse>;
