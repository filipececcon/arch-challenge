using System.Security.Claims;
using ArchChallenge.Dashboard.Application.DailyBalances;
using ArchChallenge.Dashboard.Application.DailyBalances.GetDailyBalanceByDate;
using ArchChallenge.Dashboard.Application.DailyBalances.ListDailyBalances;

namespace ArchChallenge.Dashboard.Api.Controllers;

[ApiController]
[Route("api/daily-balances")]
[Produces("application/json")]
[Authorize]
public class DailyBalancesController(IMediator mediator) : ControllerBase
{
    /// <summary>Consolidado de um dia (UTC), se existir.</summary>
    [HttpGet("{date}")]
    [ProducesResponseType(typeof(DailyBalanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByDate(DateOnly date, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub")
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? string.Empty;

        var result = await mediator.Send(new GetDailyBalanceByDateQuery(date, userId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Lista consolidados por intervalo de datas (UTC) para o usuário autenticado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DailyBalanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub")
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? string.Empty;

        var result = await mediator.Send(new ListDailyBalancesQuery(from, to, userId), cancellationToken);
        return Ok(result);
    }
}
