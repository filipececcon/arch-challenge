using ArchChallenge.CashFlow.Api.Extensions;
using ArchChallenge.CashFlow.Application.Accounts.Commands.ActivateAccount;
using ArchChallenge.CashFlow.Application.Accounts.Commands.CreateAccount;
using ArchChallenge.CashFlow.Application.Accounts.Commands.DeactivateAccount;
using ArchChallenge.CashFlow.Application.Accounts.Queries.GetMyAccount;
using ArchChallenge.CashFlow.Application.Common.Responses;

namespace ArchChallenge.CashFlow.Api.Controllers;

[ApiController]
[Route("api/accounts")]
[Produces("application/json")]
[Authorize]
public class AccountsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Cria a conta corrente para o usuário autenticado (JWT <c>sub</c>).
    /// Retorna 409 Conflict se já existir uma conta para este usuário.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Result<CreateAccountResult>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<CreateAccountResult>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var command  = new CreateAccountCommand { UserId = UserIdentity.ResolveUserId(User) };
        var envelope = await mediator.Send(command, cancellationToken);

        if (envelope.IsSuccess)
            return CreatedAtAction(nameof(GetMe), null, envelope);

        return envelope.ToActionResult();
    }

    /// <summary>
    /// Retorna os dados da conta corrente do usuário autenticado: saldo, status e datas.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(GetMyAccountResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMyAccountQuery(UserIdentity.ResolveUserId(User)), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Desativa (soft-delete) a conta corrente do usuário autenticado.
    /// Transações futuras serão rejeitadas enquanto a conta estiver inativa.
    /// </summary>
    [HttpPatch("me/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(CancellationToken cancellationToken)
    {
        var command = new DeactivateAccountCommand { UserId = UserIdentity.ResolveUserId(User) };
        var found   = await mediator.Send(command, cancellationToken);
        return found ? NoContent() : NotFound();
    }

    /// <summary>
    /// Reativa uma conta corrente previamente desativada.
    /// </summary>
    [HttpPatch("me/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(CancellationToken cancellationToken)
    {
        var command = new ActivateAccountCommand { UserId = UserIdentity.ResolveUserId(User) };
        var found   = await mediator.Send(command, cancellationToken);
        return found ? NoContent() : NotFound();
    }
}

