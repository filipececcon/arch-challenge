using ArchChallenge.CashFlow.Application.Common.Outbox;
using ArchChallenge.CashFlow.Application.Common.Responses;

namespace ArchChallenge.CashFlow.Application.Common.Handlers;

/// <summary>
/// Template Method para handlers de escrita com resposta imediata ao chamador
/// (sem <see cref="IAsyncCommand"/> / task-cache). Inclui <see cref="ResultBuilder"/> por request;
/// o filho usa <see cref="AddError"/> e <see cref="HasErrors"/>; após <see cref="ExecuteAsync"/>
/// aplica-se <see cref="CommandHandlerValidation.AppendToBuilder"/> e, se houver erros, não corre outbox/commit.
/// </summary>
public abstract class SyncCommandHandler<TAggregate, TProjection, TCommand, TResult>(
    IUnitOfWork unitOfWork,
    IOutboxRepository outboxRepository,
    IOutboxMapper<TCommand, TAggregate, TProjection> outboxMapper,
    IStringLocalizer<Messages> localizer)
    : IRequestHandler<TCommand, TResult>
    where TCommand : CommandBase, IRequest<TResult>
    where TAggregate : Entity, IAggregateRoot
    where TProjection : Entity
{
    private readonly OutboxWriter<TCommand, TAggregate, TProjection> _outboxWriter =
        new(outboxRepository, outboxMapper);

    /// <summary>Acumulador instanciado no início de cada <see cref="Handle"/>.</summary>
    private ResultBuilder _resultBuilder = null!;

    protected void AddError(string message) => _resultBuilder.AddError(message);

    protected bool HasErrors => _resultBuilder.HasErrors;

    protected abstract Task<TAggregate?> ExecuteAsync(TCommand command, CancellationToken cancellationToken);

    protected virtual TProjection GetProjection(TAggregate entity)
        => entity as TProjection
        ?? throw new InvalidOperationException(
            $"Override {nameof(GetProjection)} when TAggregate ({typeof(TAggregate).Name}) " +
            $"differs from TProjection ({typeof(TProjection).Name}).");

    protected abstract TResult BuildSuccessResult(TCommand command, TAggregate entity, TProjection projection);

    protected abstract TResult BuildFailureResult(TCommand command, ResultBuilder errors);

    public async Task<TResult> Handle(TCommand command, CancellationToken cancellationToken)
    {
        await using var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            _resultBuilder = new ResultBuilder();

            var entity = await ExecuteAsync(command, cancellationToken);

            CommandHandlerValidation.AppendToBuilder(localizer, entity, _resultBuilder);

            if (_resultBuilder.HasErrors)
            {
                await tx.RollbackAsync(cancellationToken);
                return BuildFailureResult(command, _resultBuilder);
            }

            var projection = GetProjection(entity!);

            await _outboxWriter.WriteAsync(entity!, projection, command, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);

            return BuildSuccessResult(command, entity!, projection);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
