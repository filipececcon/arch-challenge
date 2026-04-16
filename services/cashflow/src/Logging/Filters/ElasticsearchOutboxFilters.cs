using Serilog.Events;

namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Logging.Filters;

internal static class ElasticsearchOutboxFilters
{
    /// <summary>Exclui do Elasticsearch os logs de snapshot por ciclo de poll (mantidos só no console no perfil Local).</summary>
    public static bool ExcludePollCycleLog(LogEvent logEvent)
    {
        if (!logEvent.Properties.TryGetValue(OutboxLoggingConstants.OutboxPollCycle, out var p))
            return false;

        return p is ScalarValue { Value: true };
    }
}
