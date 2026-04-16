using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ArchChallenge.CashFlow.Application.Common.Behaviors;

/// <summary>
/// Pipeline MediatR que adiciona contexto de rastreamento e logs de Debug em todas as requests.
///
/// Para cada request:
///   - Abre um <c>BeginScope</c> com <c>MediatRRequest</c> (nome do tipo) e,
///     quando disponível, <c>TaskId</c> (<see cref="IAsyncCommand"/>).
///     O Serilog captura o escopo e inclui as propriedades nos eventos filhos.
///   - Emite Debug de entrada com os identificadores.
///   - Emite Debug de saída com a duração (ms).
///   - Em caso de exceção, emite Warning antes de relançar para correlacionar
///     com o LogError do ExceptionMiddleware.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var taskId      = (request as IAsyncCommand)?.TaskId;

        using var scope = BeginScope(requestName, taskId);

        logger.LogDebug("[{RequestName}] started. TaskId={TaskId}", requestName, taskId);

        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next(cancellationToken);

            sw.Stop();
            logger.LogDebug("[{RequestName}] completed in {ElapsedMs}ms. TaskId={TaskId}",
                requestName, sw.ElapsedMilliseconds, taskId);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogWarning(ex,
                "[{RequestName}] failed after {ElapsedMs}ms. TaskId={TaskId}",
                requestName, sw.ElapsedMilliseconds, taskId);
            throw;
        }
    }

    private IDisposable? BeginScope(string requestName, Guid? taskId)
    {
        return taskId.HasValue
            ? logger.BeginScope(new Dictionary<string, object>
            {
                ["MediatRRequest"] = requestName,
                ["TaskId"]         = taskId.Value
            })
            : logger.BeginScope(new Dictionary<string, object>
            {
                ["MediatRRequest"] = requestName
            });
    }
}
