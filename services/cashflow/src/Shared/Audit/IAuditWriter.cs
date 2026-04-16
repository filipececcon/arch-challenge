namespace ArchChallenge.CashFlow.Domain.Shared.Audit;

/// <summary>Grava registros de auditoria no banco imutável via SQL estruturado por raiz de agregação.</summary>
public interface IAuditWriter
{
    /// <summary>
    /// Persiste uma entrada de auditoria na tabela <c>TB_AUDIT_{AggregateType}</c>.
    /// A tabela é criada automaticamente na primeira chamada para cada tipo de agregado.
    /// </summary>
    Task WriteAuditEntryAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}
