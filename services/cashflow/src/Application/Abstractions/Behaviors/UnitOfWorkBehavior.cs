using ArchChallenge.CashFlow.Application.Abstractions.Commands;
using ArchChallenge.CashFlow.Application.Abstractions.Results;

namespace ArchChallenge.CashFlow.Application.Abstractions.Behaviors;

/// <summary>
/// Pipeline MediatR que gerencia transação para comandos marcados com <see cref="ICommand"/>.
/// Abre a transação, executa o handler e, se o resultado for sucesso, salva e commita.
/// Se o resultado for falha ou ocorrer exceção, faz rollback.
/// </summary>
public sealed class UnitOfWorkBehavior<TCommand, TResult>(IUnitOfWork unitOfWork) : IPipelineBehavior<TCommand, TResult>
    where TCommand : ICommand
{
    public async Task<TResult> Handle(TCommand command, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        await using var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await next(cancellationToken);

            if (result is IResult { IsFailure: true })
            {
                await tx.RollbackAsync(cancellationToken);
                
                return result;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            
            await tx.CommitAsync(cancellationToken);

            return result;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            
            throw;
        }
    }
}
