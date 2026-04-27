using ArchChallenge.CashFlow.Application.Abstractions.Results;
using ArchChallenge.CashFlow.Application.Accounts.Activate;
using ArchChallenge.CashFlow.Application.Accounts.Create;
using ArchChallenge.CashFlow.Application.Accounts.Deactivate;
using ArchChallenge.CashFlow.Application.Accounts.GetMyAccount;

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
        var command  = new CreateAccountCommand();
        
        var envelope = await mediator.Send(command, cancellationToken);

        if (envelope.IsSuccess) return CreatedAtAction(nameof(GetMe), null, envelope);

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
        var query = new GetMyAccountQuery(UserIdentity.ResolveUserId(User));
        
        var result = await mediator.Send(query, cancellationToken);
        
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
        var command  = new DeactivateAccountCommand();
        
        var envelope = await mediator.Send(command, cancellationToken);
        
        return envelope.ToActionResult();
    }

    /// <summary>
    /// Reativa uma conta corrente previamente desativada.
    /// </summary>
    [HttpPatch("me/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(CancellationToken cancellationToken)
    {
        var command  = new ActivateAccountCommand();
        
        var envelope = await mediator.Send(command, cancellationToken);
        
        return envelope.ToActionResult();
    }
}

