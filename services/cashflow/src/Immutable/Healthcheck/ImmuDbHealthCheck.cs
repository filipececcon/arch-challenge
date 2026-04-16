using ImmuDB;

namespace ArchChallenge.CashFlow.Infrastructure.Data.Immutable.Healthcheck;

/// <summary>
/// Verifica conectividade com o immudb abrindo e fechando uma sessão (sem gravar dados).
/// </summary>
public sealed class ImmuDbHealthCheck(IOptions<ImmuDbOptions> options, ILogger<ImmuDbHealthCheck> logger)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken  cancellationToken = default)
    {
        var opt = options.Value;
        ImmuClient? client = null;

        try
        {
            client = ImmuClient.NewBuilder()
                .WithServerUrl(opt.Host)
                .WithServerPort(opt.Port)
                .CheckDeploymentInfo(opt.CheckDeploymentInfo)
                .Build();

            await client.Open(opt.Username, opt.Password, opt.Database).ConfigureAwait(false);

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ImmuDB health check failed.");
            return HealthCheckResult.Unhealthy(exception: ex);
        }
        finally
        {
            if (client is not null)
            {
                try
                {
                    await client.Close().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "ImmuDB health check Close ignored.");
                }

                try
                {
                    await ImmuClient.ReleaseSdkResources().ConfigureAwait(false);
                }
                catch
                {
                    /* ignore */
                }
            }
        }
    }
}
