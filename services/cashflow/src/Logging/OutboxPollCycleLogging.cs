using Serilog.Context;

namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Logging;

/// <summary>Inicia um escopo Serilog que marca logs de snapshot do outbox (excluídos do sink Elasticsearch).</summary>
public static class OutboxPollCycleLogging
{
    public static IDisposable BeginPollCycleScope()
        => LogContext.PushProperty(OutboxLoggingConstants.OutboxPollCycle, true);
}
