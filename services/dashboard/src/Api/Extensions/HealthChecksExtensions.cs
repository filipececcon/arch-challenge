using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using RabbitMQ.Client;

namespace ArchChallenge.Dashboard.Api.Extensions;

public static class HealthChecksExtensions
{
    private const string ReadyTag = "ready";

    public static IServiceCollection AddHealthChecksConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddHealthChecks()
            .AddMongoDb(
                _ => new MongoClient(configuration.GetConnectionString("MongoConnection")),
                name: "mongodb",
                tags: [ReadyTag])
            .AddRabbitMQ(
                async _ =>
                {
                    var factory = new ConnectionFactory
                    {
                        HostName = configuration["RabbitMQ:Host"]!,
                        UserName = configuration["RabbitMQ:Username"]!,
                        Password = configuration["RabbitMQ:Password"]!
                    };
                    return await factory.CreateConnectionAsync();
                },
                name: "rabbitmq",
                tags: [ReadyTag]);

        return services;
    }

    public static IEndpointRouteBuilder MapHealthCheckEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/liveness", new HealthCheckOptions
        {
            Predicate       = _ => false,
            ResponseWriter  = WriteJsonResponse
        });

        endpoints.MapHealthChecks("/health/readiness", new HealthCheckOptions
        {
            Predicate      = hc => hc.Tags.Contains(ReadyTag),
            ResponseWriter = WriteJsonResponse
        });

        return endpoints;
    }

    private static Task WriteJsonResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        using var stream  = new MemoryStream();
        using var writer  = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteString("status", report.Status.ToString());
        writer.WriteString("totalDuration", report.TotalDuration.ToString());
        writer.WriteStartObject("checks");

        foreach (var (name, entry) in report.Entries)
        {
            writer.WriteStartObject(name);
            writer.WriteString("status", entry.Status.ToString());
            writer.WriteString("duration", entry.Duration.ToString());
            if (entry.Description is not null)
                writer.WriteString("description", entry.Description);
            if (entry.Exception is not null)
                writer.WriteString("error", entry.Exception.Message);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.Flush();

        return context.Response.WriteAsync(Encoding.UTF8.GetString(stream.ToArray()));
    }
}
