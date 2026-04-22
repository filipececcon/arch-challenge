namespace ArchChallenge.CashFlow.Application.Accounts.Commands.CreateAccount;

/// <summary>
/// Cria a conta corrente do usuário autenticado (JWT <c>sub</c>).
/// Retorna erro de domínio se já existir uma conta para este usuário.
/// </summary>
public record CreateAccountCommand : CommandBase, IRequest<CreateAccountResult>;
