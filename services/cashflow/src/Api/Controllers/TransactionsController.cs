using ArchChallenge.CashFlow.Api.Filters;
using ArchChallenge.CashFlow.Application.Transactions.Enqueue;
using ArchChallenge.CashFlow.Application.Transactions.GetAll;
using ArchChallenge.CashFlow.Application.Transactions.GetById;

namespace ArchChallenge.CashFlow.Api.Controllers;

[ApiController]
[Route("api/accounts/{accountId:guid}/transactions")]
[Produces("application/json")]
[Authorize]
public class TransactionsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Valida e enfileira o processamento de uma transação financeira na conta identificada por <paramref name="accountId"/>.
    /// A conta deve pertencer ao usuário autenticado (JWT <c>sub</c>).
    /// Retorna um taskId para acompanhamento assíncrono via SSE.
    /// O header <c>Idempotency-Key</c> deve estar presente; se vazio, trata-se de nova operação; se UUID, deduplicação.
    /// </summary>
    [HttpPost]
    [RequireIdempotencyKeyHeader]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        Guid accountId,
        [FromBody] EnqueueTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            command with { IdempotencyKey = HttpContext.GetIdempotencyKey() },
            cancellationToken);

        return Accepted(new { result.Data!.TaskId });
    }

    /// <summary>Retorna uma transação pelo seu identificador (apenas na conta indicada e do usuário autenticado).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetTransactionByIdResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid accountId, Guid id, CancellationToken cancellationToken)
    {
        var userId = UserIdentity.ResolveUserId(User);

        var result = await mediator.Send(new GetTransactionByIdQuery(id, userId), cancellationToken);
        
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Lista transações (read model Mongo) da conta indicada, com filtros opcionais na query string:
    /// <c>type</c>, <c>active</c>, <c>minAmount</c>, <c>maxAmount</c>, <c>createdFrom</c>, <c>createdTo</c>.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetAllTransactionsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
        Guid accountId,
        [FromQuery] string? type,
        [FromQuery] bool? active,
        [FromQuery] decimal? minAmount,
        [FromQuery] decimal? maxAmount,
        [FromQuery] DateTime? createdFrom,
        [FromQuery] DateTime? createdTo,
        CancellationToken cancellationToken)
    {
        var userId = UserIdentity.ResolveUserId(User);
        
        var query = new GetAllTransactionsQuery(
            userId,
            type,
            active,
            minAmount,
            maxAmount,
            createdFrom,
            createdTo);

        var result = await mediator.Send(query, cancellationToken);
        
        return Ok(result);
    }
}
