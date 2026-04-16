using System.Reflection;
using ArchChallenge.Dashboard.Application.Common.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace ArchChallenge.Dashboard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }
}
