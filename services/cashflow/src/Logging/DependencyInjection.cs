using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Logging.Filters;
using Elastic.Apm.SerilogEnricher;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Logging;

public static class DependencyInjection
{
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog((provider, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(provider)
                .Filter.ByExcluding(logEvent =>
                    logEvent.Level <= LogEventLevel.Information
                    && SerilogEfOutboxFilters.ExcludeTaggedOutboxWorkerSql(logEvent))
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.WithElasticApmCorrelationInfo();

            var esSection = configuration.GetSection("ElasticsearchLogging");
            var nodeUri = esSection["NodeUri"];

            if (string.IsNullOrEmpty(nodeUri)) return;

            var username = esSection["Username"];
            var password = esSection["Password"];
            var indexFormat = esSection["IndexFormat"] ?? "logs-{0:yyyy.MM.dd}";

            loggerConfiguration.WriteTo.Logger(lc => lc
                .Filter.ByExcluding(ElasticsearchOutboxFilters.ExcludePollCycleLog)
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(nodeUri))
                {
                    IndexFormat = indexFormat,
                    ModifyConnectionSettings = conn => string.IsNullOrEmpty(username)
                        ? conn
                        : conn.BasicAuthentication(username, password)
                }));
        });

        services.AddAllElasticApm();

        return services;
    }
}
