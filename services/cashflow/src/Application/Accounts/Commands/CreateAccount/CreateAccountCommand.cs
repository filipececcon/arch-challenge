using ArchChallenge.CashFlow.Application.Common.Responses;

namespace ArchChallenge.CashFlow.Application.Accounts.Commands.CreateAccount;

/// <summary>
/// Cria a conta corrente do usuário autenticado (JWT <c>sub</c>).
/// Retorna <c>Result&lt;CreateAccountResult&gt;</c>; conflito (conta existente) vem com HTTP 409.
/// </summary>
public record CreateAccountCommand : CommandBase, IRequest<Result<CreateAccountResult>>;
