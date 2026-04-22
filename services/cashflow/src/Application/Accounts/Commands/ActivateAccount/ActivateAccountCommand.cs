namespace ArchChallenge.CashFlow.Application.Accounts.Commands.ActivateAccount;

/// <summary>Reativa uma conta corrente previamente desativada.</summary>
public record ActivateAccountCommand : CommandBase, IRequest<bool>;
