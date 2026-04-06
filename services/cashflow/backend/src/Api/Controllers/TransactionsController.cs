using ArchChallenge.CashFlow.Application.Transactions.Commands.RegisterTransaction;
using ArchChallenge.CashFlow.Application.Transactions.Queries.GetTransactionById;
using ArchChallenge.CashFlow.Application.Transactions.Queries.ListTransactions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ArchChallenge.CashFlow.Api.Controllers;

[ApiController]
[Route("api/transactions")]
[Produces("application/json")]
public class TransactionsController(IMediator mediator) : ControllerBase
{
    /// <summary>Registers a new financial transaction (credit or debit).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(RegisterTransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] RegisterTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { type = "DomainError", errors = result.Errors.Select(e => new { field = e.Field, message = e.Message }) });

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>Returns a transaction by its identifier.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTransactionByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Lists all transactions.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListTransactionsQuery(), cancellationToken);
        return Ok(result);
    }

}
