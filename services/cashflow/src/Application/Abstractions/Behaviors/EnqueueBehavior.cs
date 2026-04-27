using ArchChallenge.CashFlow.Application.Abstractions.Responses;
using ArchChallenge.CashFlow.Application.Abstractions.Results;
using ArchChallenge.CashFlow.Application.Abstractions.Tasks;
using Microsoft.Extensions.Localization;

namespace ArchChallenge.CashFlow.Application.Abstractions.Behaviors;

/// <summary>
/// Gerencia o ciclo de vida do TaskId e a idempotência para comandos de enfileiramento (<see cref="IEnqueueCommand{TResponse}"/>).
///
/// Fluxo:
///   1. Se <c>IdempotencyKey</c> fornecida e já existe no cache → retorna <c>Result.Ok</c> com o taskId original (short-circuit).
///   2. Gera um novo <c>TaskId</c> e o injeta no command.
///   3. Persiste o status <c>Pending</c> no cache.
///   4. Chama o handler (que publica a mensagem no broker).
///   5. Se <c>IdempotencyKey</c> fornecida → associa idempotencyKey → taskId no cache (TTL 24 h).
///   6. Em caso de falha → <c>SetFailure</c> no cache; a task nunca ficará travada em Pending.
///
/// O worker/consumer que processa a mensagem é responsável por chamar
/// <c>SetSuccessAsync</c> ou <c>SetFailureAsync</c> ao concluir o processamento.
///
/// Pipeline resultante:
///   Logging → Validation → EnqueueTaskCache → Handler
/// </summary>
public sealed class EnqueueBehavior<TCommand, TResponse>(
    ITaskCacheService taskCache,
    IStringLocalizer<Messages> localizer)
    : IPipelineBehavior<TCommand, Result<TResponse>>
    where TCommand : IEnqueueCommand<TResponse>
    where TResponse : class, IEnqueueResponse
{
    public async Task<Result<TResponse>> Handle(
        TCommand command,
        RequestHandlerDelegate<Result<TResponse>> next,
        CancellationToken cancellationToken)
    {
        // 1. Verificar idempotência
        if (command.IdempotencyKey is { } key)
        {
            var existingTaskId = await taskCache.GetIdempotencyAsync(key, cancellationToken);

            if (existingTaskId is not null)
            {
                // Já foi enfileirado anteriormente — retorna o taskId original sem re-publicar.
                var existing = CreateEnqueueResponse<TResponse>(existingTaskId.Value);

                return Result<TResponse>.Ok(existing, 202);
            }
        }

        // 2. Gerar e injetar TaskId
        var taskId = Guid.NewGuid();
        
        command.TaskId = taskId;

        // 3. Marcar como pendente antes de publicar
        await taskCache.SetPendingAsync(taskId, cancellationToken);

        try
        {
            var result = await next(cancellationToken);

            if (result.IsFailure)
            {
                await taskCache.SetFailureAsync(taskId, result.Errors, cancellationToken);
                return result;
            }

            // 5. Associar idempotencyKey → taskId
            if (command.IdempotencyKey is { } newKey)
                await taskCache.SetIdempotencyAsync(newKey, taskId, cancellationToken);

            return result;
        }
        catch
        {
            await taskCache.SetFailureAsync(taskId,
                [localizer[MessageKeys.Exception.InternalError].Value], cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Cria a resposta tipada para o short-circuit de idempotência.
    /// A constraint <c>IEnqueueResponse</c> garante que <typeparamref name="TResponse"/>
    /// possui um construtor que aceita um único <see cref="Guid"/>.
    /// </summary>
    private static TResponse CreateEnqueueResponse<T>(Guid taskId) where T : class, IEnqueueResponse
    {
        var ctor = typeof(TResponse).GetConstructor([typeof(Guid)]);

        if (ctor is null)
            throw new InvalidOperationException(
                $"Type '{typeof(TResponse).Name}' must have a constructor that accepts a single Guid (taskId).");

        return (TResponse)ctor.Invoke([taskId]);
    }
}
