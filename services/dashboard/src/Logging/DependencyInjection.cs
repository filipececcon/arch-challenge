using Elastic.Apm.SerilogEnricher;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace ArchChallenge.Dashboard.Infrastructure.CrossCutting.Logging;

public static class DependencyInjection
{
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog((provider, lc) =>
        {
            lc
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(provider)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.WithElasticApmCorrelationInfo();

            var esSection = configuration.GetSection("ElasticsearchLogging");
            var nodeUri   = esSection["NodeUri"];

            if (string.IsNullOrEmpty(nodeUri)) return;

            var username    = esSection["Username"];
            var password    = esSection["Password"];
            var indexFormat = esSection["IndexFormat"] ?? "dashboard-api";

            lc.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(nodeUri))
            {
                IndexFormat = indexFormat,
                ModifyConnectionSettings = conn => string.IsNullOrEmpty(username)
                    ? conn
                    : conn.BasicAuthentication(username, password)
            });
        });

        services.AddAllElasticApm();

        return services;
    }
}
