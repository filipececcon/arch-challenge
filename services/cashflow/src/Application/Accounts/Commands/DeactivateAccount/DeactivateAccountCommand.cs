namespace ArchChallenge.CashFlow.Application.Accounts.Commands.DeactivateAccount;

/// <summary>Desativa (soft-delete) a conta corrente do usuário autenticado.</summary>
public record DeactivateAccountCommand : CommandBase, IRequest<bool>;
