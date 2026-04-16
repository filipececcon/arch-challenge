using System.Text.Json;

namespace ArchChallenge.CashFlow.Application.Common.Commands;

/// <summary>
/// Resultado retornado pelo método <c>ExecuteAsync</c> do <see cref="CommandHandlerBase{TCommand,TAggregate,TMessage}"/>.
/// Carrega tudo que o template de infraestrutura precisa para finalizar o fluxo:
/// auditoria, task cache e ação pós-commit opcional.
/// </summary>
public sealed record CommandResult<TAggregate>(
    TAggregate                      Aggregate,
    string                          EventName,
    JsonElement                     Payload,
    Func<CancellationToken, Task>?  AfterCommit = null)
    where TAggregate : IAggregateRoot;
