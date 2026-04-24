using ArchChallenge.CashFlow.Application.Abstractions.Results;

namespace ArchChallenge.CashFlow.Application.Abstractions.Commands;

/// <summary>
/// Marker para comandos transacionais — o <c>UnitOfWorkBehavior</c> abre e gerencia
/// a transação automaticamente para todo request que implemente esta interface.
/// </summary>
public interface ICommand : IAuditable;

/// <summary>
/// Comando transacional tipado que retorna <see cref="Result{TResponse}"/>.
/// </summary>
public interface ICommand<TResponse> : ICommand, IRequest<Result<TResponse>> where TResponse : class;
