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
            // Logging → Validation → TaskCache → UnitOfWork → Outbox → Handler
            //
            // TaskCache aplica-se somente a ITrackedCommand.
            // UnitOfWork e Outbox aplicam-se somente a ISyncCommand.
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(TaskCacheBehavior<,>));
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
