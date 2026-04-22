using ArchChallenge.CashFlow.Application.Common.Behaviors;
using ArchChallenge.CashFlow.Application.Common.Enqueue;
using ArchChallenge.CashFlow.Application.Transactions.Commands.EnqueueTransaction;
using ArchChallenge.CashFlow.Application.Transactions.Commands.ExecuteTransaction;

namespace ArchChallenge.CashFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<
            IOutboxMapper<ExecuteTransactionCommand, Account, Transaction>,
            ExecuteTransactionOutboxMapper>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ExecuteTransactionHandler).Assembly));

        // O EnqueueCommandHandler é genérico aberto e não é descoberto pelo scanner.
        // Cada command que implementa IEnqueueCommand<TMessage> precisa ser registrado aqui.
        services.AddTransient<
            IRequestHandler<EnqueueTransactionCommand, EnqueueResult>,
            EnqueueCommandHandler<EnqueueTransactionCommand, EnqueueTransactionMessage>>();

        services.AddValidatorsFromAssembly(typeof(EnqueueTransactionValidator).Assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
