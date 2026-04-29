using ArchChallenge.CashFlow.Application.Abstractions.Behaviors;
using ArchChallenge.CashFlow.Application.Abstractions.Outbox;

namespace ArchChallenge.CashFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

            // Pipeline (mais externo → mais interno):
            // Identity → Logging → Validation → EnqueueTaskCache* → TaskCache** → UnitOfWork → Outbox → Handler
            cfg.AddOpenBehavior(typeof(IdentityBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(EnqueueBehavior<,>));
            cfg.AddOpenBehavior(typeof(UnitOfWorkBehavior<,>));
            cfg.AddOpenBehavior(typeof(OutboxBehavior<,>));
        });

        services.AddValidatorsFromAssembly(
            typeof(DependencyInjection).Assembly,
            includeInternalTypes: true);

        services.AddScoped<IOutboxContext, OutboxContext>();

        return services;
    }
}
