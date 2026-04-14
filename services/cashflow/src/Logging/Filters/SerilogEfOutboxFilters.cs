using ArchChallenge.CashFlow.Domain.Shared.Logging;
using Serilog.Events;

namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Logging.Filters;

/// <summary>
/// Remove dos sinks os logs de SQL do EF marcados com <see cref="OutboxWorkerEfQueryTags"/> (worker do outbox).
/// </summary>
internal static class SerilogEfOutboxFilters
{
    /// <summary>
    /// Retorna <c>true</c> se o evento deve ser descartado (não escrito nos sinks).
    /// </summary>
    public static bool ExcludeTaggedOutboxWorkerSql(LogEvent logEvent)
    {
        if (!TryGetSourceContext(logEvent, out var source)
            || !source.Contains("Microsoft.EntityFrameworkCore.Database.Command", StringComparison.Ordinal))
            return false;

        if (logEvent.Properties.TryGetValue("commandText", out var commandTextProp))
        {
            var sql = commandTextProp.ToString().Trim('"');
            if (sql.Contains(OutboxWorkerEfQueryTags.PendingBatchQueryMarker, StringComparison.Ordinal))
                return true;
        }

        return logEvent.RenderMessage().Contains(OutboxWorkerEfQueryTags.PendingBatchQueryMarker, StringComparison.Ordinal);
    }

    private static bool TryGetSourceContext(LogEvent logEvent, out string source)
    {
        source = string.Empty;
        if (!logEvent.Properties.TryGetValue("SourceContext", out var sc))
            return false;

        source = sc.ToString().Trim('"');
        return source.Length > 0;
    }
}
