using Microsoft.Extensions.DependencyInjection;

namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.I18n;

public static class DependencyInjection
{
    public static IServiceCollection AddI18n(this IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        return services;
    }
}

