using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ArchChallenge.Dashboard.Application.Common.Behaviors;

/// <summary>
/// Pipeline MediatR que registra entrada, saída e duração de cada request.
/// Em caso de exceção emite Warning antes de relançar para correlacionar
/// com o ExceptionMiddleware.
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

        using var _ = logger.BeginScope(new Dictionary<string, object>
        {
            ["MediatRRequest"] = requestName
        });

        logger.LogDebug("[{RequestName}] started", requestName);

        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next(cancellationToken);

            sw.Stop();
            logger.LogDebug("[{RequestName}] completed in {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "[{RequestName}] failed after {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
