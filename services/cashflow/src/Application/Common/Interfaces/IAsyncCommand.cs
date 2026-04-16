namespace ArchChallenge.CashFlow.Application.Common.Interfaces;

/// <summary>
/// Marca commands de execução (processados de forma assíncrona via mensageria)
/// que possuem um <see cref="TaskId"/> para rastreamento via task cache.
/// </summary>
public interface IAsyncCommand
{
    Guid TaskId { get; }
}
