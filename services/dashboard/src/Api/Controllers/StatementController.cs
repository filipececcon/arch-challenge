using ArchChallenge.Dashboard.Application.Statement;
using ArchChallenge.Dashboard.Application.Statement.ListStatementLines;

namespace ArchChallenge.Dashboard.Api.Controllers;

[ApiController]
[Route("api/statement")]
[Produces("application/json")]
[Authorize]
public class StatementController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Extrato paginado de lançamentos processados.
    /// Filtre por <c>from</c>/<c>to</c> (DateOnly yyyy-MM-dd), <c>type</c> (CREDIT | DEBIT),
    /// <c>page</c> e <c>pageSize</c> (máx. 200, padrão 50).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(StatementPageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? type,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new ListStatementLinesQuery(from, to, type, page, pageSize),
            cancellationToken);

        return Ok(result);
    }
}
