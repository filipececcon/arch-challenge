using ArchChallenge.CashFlow.Application.Common.Behaviors;
using ArchChallenge.CashFlow.Application.Transactions.Commands.RegisterTransaction;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ArchChallenge.CashFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(RegisterTransactionHandler).Assembly));

        services.AddValidatorsFromAssembly(typeof(RegisterTransactionValidator).Assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
