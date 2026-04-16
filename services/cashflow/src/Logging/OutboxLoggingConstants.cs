namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Logging;

/// <summary>Propriedades Serilog usadas para roteamento (ex.: excluir ciclo de poll do Elasticsearch).</summary>
public static class OutboxLoggingConstants
{
    /// <summary>Marcador em <see cref="Serilog.Context.LogContext"/> — ciclo de leitura do outbox (somente console no perfil Local).</summary>
    public const string OutboxPollCycle = "OutboxPollCycle";
}
