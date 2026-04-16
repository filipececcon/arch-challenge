using ArchChallenge.CashFlow.Domain.Shared.Interfaces;

namespace ArchChallenge.CashFlow.Domain.Shared.Audit;

/// <summary>
/// Escopo por requisição/mensagem: o pipeline de auditoria define metadados antes do handler;
/// o handler chama <see cref="Capture"/> na raiz de agregação; a UoW materializa o outbox na mesma transação.
/// </summary>
public interface IAuditContext
{
    void SetMetadata(string userId, DateTime occurredAt);

    /// <summary>
    /// Registra o agregado a ser auditado e o nome do evento de domínio resultante (ex: "TransactionProcessed").
    /// O snapshot do estado é tirado após a persistência, garantindo que o Id gerado já esteja preenchido.
    /// </summary>
    void Capture(IAggregateRoot aggregate, string eventName);

    /// <summary>
    /// Materializa o payload para gravação no outbox de auditoria (idempotente até <see cref="NotifyPersisted"/>).
    /// </summary>
    bool TryBuildAuditOutboxPayload(out string eventName, out string payloadJson);

    /// <summary>Chamado pela UoW após <c>SaveChangesAsync</c> concluir com sucesso.</summary>
    void NotifyPersisted();
}
