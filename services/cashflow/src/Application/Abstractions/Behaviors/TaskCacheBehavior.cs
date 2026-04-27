using System.Text.Json;
using ArchChallenge.CashFlow.Application.Abstractions.Commands;
using ArchChallenge.CashFlow.Application.Abstractions.Results;
using ArchChallenge.CashFlow.Application.Abstractions.Tasks;
using ArchChallenge.CashFlow.Application.Abstractions.Utils;
using Microsoft.Extensions.Localization;

namespace ArchChallenge.CashFlow.Application.Abstractions.Behaviors;

/// <summary>
/// Atualiza o status da tarefa no Redis para comandos com rastreamento (<see cref="ITrackedCommand"/>).
/// Posicionado fora do <see cref="UnitOfWorkBehavior{TCommand,TResult}"/>: garante que o cache
/// só é atualizado após o commit ter ocorrido (sucesso) ou em qualquer falha.
///
/// Pipeline resultante:
///   Logging → Validation → TaskCache → UnitOfWork → Outbox → Handler
/// </summary>
public sealed class TaskCacheBehavior<TCommand, TResult>(
    ITaskCacheService taskCache,
    IStringLocalizer<Messages> localizer)
    : IPipelineBehavior<TCommand, TResult>
    where TCommand : ITrackedCommand
{
    public async Task<TResult> Handle(TCommand command, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        try
        {
            var result = await next(cancellationToken);

            if (result is IResult { IsFailure: true } failure)
            {
                await taskCache.SetFailureAsync(command.TaskId, failure.Errors, cancellationToken);
                return result;
            }

            if (result is IResult success)
            {
                var payload = JsonSerializer.SerializeToElement(success.GetData(), SerializeUtils.EntityJsonOptions);
                await taskCache.SetSuccessAsync(command.TaskId, payload, cancellationToken);
            }

            return result;
        }
        catch
        {
            await taskCache.SetFailureAsync(command.TaskId,
                [localizer[MessageKeys.Exception.InternalError].Value], cancellationToken);
            throw;
        }
    }
}
