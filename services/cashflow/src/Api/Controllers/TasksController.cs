using System.Text.Json;
using ArchChallenge.CashFlow.Application.Abstractions.Tasks;
using ArchChallenge.CashFlow.Application.Abstractions.Utils;
using TaskStatus = ArchChallenge.CashFlow.Application.Abstractions.Tasks.TaskStatus;

namespace ArchChallenge.CashFlow.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController(ITaskCacheService taskCache) : ControllerBase
{
    /// <summary>
    /// Stream SSE que acompanha o status de processamento de uma transação enfileirada.
    /// Faz polling no cache a cada 500ms até obter um estado final (Success ou Failure).
    /// </summary>
    [HttpGet("{taskId:guid}")]
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

    private async Task WriteSseAsync(object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, SerializeUtils.EntityJsonOptions);
        
        await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        
        await Response.Body.FlushAsync(cancellationToken);
    }
}
