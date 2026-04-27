namespace ArchChallenge.CashFlow.Domain.Shared.Audit;

/// <summary>
/// Marca comandos cuja execução bem-sucedida deve gerar entrada no outbox de auditoria
/// (persistida atomicamente com o agregado) e posteriormente no banco imutável.
/// </summary>
public interface IAuditable
{
    string UserId { get; set; }
    DateTime OccurredAt { get; set; }
}
