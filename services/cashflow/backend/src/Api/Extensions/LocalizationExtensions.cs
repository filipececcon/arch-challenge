using ArchChallenge.CashFlow.Api.Middlewares;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.I18n;

namespace ArchChallenge.CashFlow.Api.Extensions;

public static class LocalizationExtensions
{
    public static IServiceCollection AddLocalizationConfiguration(this IServiceCollection services)
    {
        services.AddI18n();
        return services;
    }

    public static IApplicationBuilder UseLocalizationConfiguration(this IApplicationBuilder app)
    {
        app.UseMiddleware<LocalizationMiddleware>();
        return app;
    }
}
