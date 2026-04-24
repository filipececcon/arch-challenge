using ArchChallenge.CashFlow.Application.Transactions.GetAll;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace ArchChallenge.CashFlow.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "CashFlow API",
                Version = "v1",
                Description = "Financial transaction management API (credits and debits)."
            });

            var apiXml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var apiXmlPath = Path.Combine(AppContext.BaseDirectory, apiXml);
            if (File.Exists(apiXmlPath))
                c.IncludeXmlComments(apiXmlPath);

            // Comentários dos DTOs/queries na Application (ex.: filtros GET alinhados ao FluentValidation).
            var applicationXml = $"{typeof(GetAllTransactionsQuery).Assembly.GetName().Name}.xml";
            var applicationXmlPath = Path.Combine(AppContext.BaseDirectory, applicationXml);
            if (File.Exists(applicationXmlPath))
                c.IncludeXmlComments(applicationXmlPath);
        });

        services.AddFluentValidationRulesToSwagger();

        return services;
    }

    public static IApplicationBuilder UseSwaggerConfiguration(this WebApplication app)
    {
        if (app.Environment.IsProduction()) return app;

        app.MapSwagger().AllowAnonymous();

        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "CashFlow API v1");
            c.RoutePrefix = string.Empty;
        });

        
        return app;
    }
}

