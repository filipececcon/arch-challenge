using ArchChallenge.CashFlow.Application.Transactions.Commands.EnqueueTransaction;
using ArchChallenge.CashFlow.Application.Transactions.Queries.GetAllTransactions;
using ArchChallenge.CashFlow.Application.Transactions.Queries.GetTransactionById;

namespace ArchChallenge.CashFlow.Api.Controllers;

[ApiController]
[Route("api/transactions")]
[Produces("application/json")]
[Authorize]
public class TransactionsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Valida e enfileira o processamento de uma transação financeira.
    /// Retorna um taskId para acompanhamento assíncrono via SSE.
    /// Envie o header <c>Idempotency-Key</c> (UUID) para garantir que retries
    /// do cliente não gerem transações duplicadas.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] EnqueueTransaction command,
        [FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            command with { IdempotencyKey = idempotencyKey },
            cancellationToken);

        return Accepted(new { result.TaskId });
    }

    /// <summary>Retorna uma transação pelo seu identificador.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetTransactionByIdResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTransactionByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Lista transações (read model Mongo) com filtros opcionais na query string:
    /// <c>type</c>, <c>active</c>, <c>minAmount</c>, <c>maxAmount</c>, <c>createdFrom</c>, <c>createdTo</c>.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetAllTransactionsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List([FromQuery] GetAllTransactionsQuery query, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
