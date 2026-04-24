namespace ArchChallenge.CashFlow.Application.Abstractions.Queries;

/// <summary>
/// Marker para queries de leitura — o <c>ReadOnlyBehavior</c> configura o DbContext para não rastrear as
/// entidades carregadas para todo request que implemente esta interface. 
/// </summary>
/// <typeparam name="TResponse"></typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>;
