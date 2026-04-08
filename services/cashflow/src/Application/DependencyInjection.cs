using ArchChallenge.CashFlow.Application.Common.Behaviors;
using ArchChallenge.CashFlow.Application.Common.Enqueue;
using ArchChallenge.CashFlow.Application.Common.Interfaces;
using ArchChallenge.CashFlow.Application.Transactions.Commands.CreateTransaction;
using ArchChallenge.CashFlow.Application.Transactions.Commands.ExecuteTransaction;

namespace ArchChallenge.CashFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ExecuteTransactionHandler).Assembly));

        // O EnqueueCommandHandler é genérico aberto e não é descoberto pelo scanner.
        // Cada command que implementa IEnqueueCommand<TMessage> precisa ser registrado aqui.
        services.AddTransient<
            IRequestHandler<CreateTransactionCommand, EnqueueResult>,
            EnqueueCommandHandler<CreateTransactionCommand, CreateTransactionMessage>>();

        services.AddValidatorsFromAssembly(typeof(CreateTransactionValidator).Assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
