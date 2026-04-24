using ArchChallenge.CashFlow.Application.Common.Responses;

namespace ArchChallenge.CashFlow.Application.Common.Handlers;

/// <summary>
/// Valida entidades retornadas por repositórios em handlers de comando,
/// construindo mensagens de erro legíveis para o cliente a partir de notificações de falha
/// (ex.: validação de invariantes de domínio). O resultado é uma lista de mensagens de erro, ou vazia se a entidade
/// for válida. O template de handler de comando pode usar esta classe para construir a resposta de falha quando o
/// repositório não encontrar a entidade ou quando o agregado retornar notificações de falha.
/// O <see cref="SyncCommandHandler{TAggregate,TProjection,TCommand,TResult}"/> também expõe <c>AddError</c> para acumular erros específicos do
/// comando, e rejeitar a operação se houver erros acumulados.
/// </summary>
internal static class CommandHandlerValidation
{
    public static IReadOnlyList<string> BuildMessages<TAggregate>(IStringLocalizer<Messages> localizer, TAggregate? entity)
        where TAggregate : Entity, IAggregateRoot
    {
        if (entity is null)
            return [localizer[MessageKeys.Validation.EntityNotFound].Value];

        if (entity.IsFailure)
            return entity.Notifications
                .Select(n => $"{n.Key} {n.Message}".Trim())
                .ToArray();

        return [];
    }

    /// <summary>
    /// Junta a saída de <see cref="BuildMessages"/> ao acumulador.
    /// Se a entidade for nula e o acumulador já tiver erros (regra preenchida no filho, ex. conflito 409), não
    /// acrescenta a mensagem de "não encontrado".
    /// </summary>
    public static void AppendToBuilder<TAggregate>(
        IStringLocalizer<Messages> localizer,
        TAggregate?                  entity,
        ResultBuilder                errorBuilder)
        where TAggregate : Entity, IAggregateRoot
    {
        if (entity is null && errorBuilder.HasErrors)
            return;

        foreach (var m in BuildMessages(localizer, entity))
            errorBuilder.AddError(m);
    }
}
