namespace ArchChallenge.Dashboard.Domain.Entities;

/// <summary>Saldo agregado por dia (UTC), derivado dos eventos TransactionRegistered.</summary>
public class DailyConsolidation
{
    public DateOnly Date { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal TotalDebits { get; set; }
    public DateTime UpdatedAt { get; set; }
}
