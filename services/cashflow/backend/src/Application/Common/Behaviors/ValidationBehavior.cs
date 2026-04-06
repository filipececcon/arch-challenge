using ArchChallenge.CashFlow.Domain.Shared.Notifications;
using FluentValidation;
using MediatR;

namespace ArchChallenge.CashFlow.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count == 0)
            return await next(cancellationToken);

        var notifications = failures
            .Select(f => Notification.Create(f.PropertyName, f.ErrorMessage))
            .ToList();

        if (IsResultType(typeof(TResponse)))
            return CreateFailureResult(notifications);

        throw new ValidationException(failures);
    }

    private static bool IsResultType(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>);

    private static TResponse CreateFailureResult(IReadOnlyList<Notification> notifications)
    {
        var failureMethod = typeof(TResponse).GetMethod(
            nameof(Result<object>.Failure),
            [typeof(IReadOnlyList<Notification>)]);

        return (TResponse)failureMethod!.Invoke(null, [notifications])!;
    }
}
