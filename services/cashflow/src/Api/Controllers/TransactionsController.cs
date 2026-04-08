using System.Text.Json;
using ArchChallenge.CashFlow.Application.Common.Interfaces;
using ArchChallenge.CashFlow.Application.Common.Tasks;
using ArchChallenge.CashFlow.Application.Transactions.Commands.CreateTransaction;
using ArchChallenge.CashFlow.Application.Transactions.Queries.GetTransactionById;
using ArchChallenge.CashFlow.Application.Transactions.Queries.ListTransactions;
using TaskStatus = ArchChallenge.CashFlow.Application.Common.Tasks.TaskStatus;

namespace ArchChallenge.CashFlow.Api.Controllers;

[ApiController]
[Route("api/transactions")]
[Produces("application/json")]
public class TransactionsController(
    IMediator mediator,
    ITaskCacheService taskCache) : ControllerBase
{
    private static readonly JsonSerializerOptions SseJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Valida e enfileira o processamento de uma transação financeira.
    /// Retorna um taskId para acompanhamento assíncrono via SSE.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTransactionCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        
        return Accepted(new { result.TaskId });
    }

    /// <summary>
    /// Stream SSE que acompanha o status de processamento de uma transação enfileirada.
    /// Faz polling no cache a cada 500ms até obter um estado final (Success ou Failure).
    /// </summary>
    [HttpGet("tasks/{taskId:guid}/status")]
    [Produces("text/event-stream")]
    public async Task GetTaskStatus(Guid taskId, CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await taskCache.GetAsync(taskId, cancellationToken);

                if (result is null)
                {
                    await WriteSseAsync(new { status = "not_found", taskId }, cancellationToken);
                    return;
                }

                await WriteSseAsync(result, cancellationToken);

                if (result.Status != TaskStatus.Pending)
                    return;

                await Task.Delay(500, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // cliente desconectou — encerra silenciosamente
        }
    }

    /// <summary>Retorna uma transação pelo seu identificador.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTransactionByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Lista todas as transações.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListTransactionsQuery(), cancellationToken);
        return Ok(result);
    }

    private async Task WriteSseAsync(object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, SseJsonOptions);
        await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
