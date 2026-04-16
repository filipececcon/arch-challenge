namespace ArchChallenge.CashFlow.Infrastructure.Data.Immutable.Conventions;

/// <summary>
/// Convenção de nomenclatura de tabelas de auditoria no immudb.
/// Cada raiz de agregação tem sua própria tabela (<c>TB_AUDIT_{AGGREGATE}</c>).
/// </summary>
public static class AuditTableConventions
{
    private const string Prefix = "TB_AUDIT_";

    /// <summary>Resolve o nome da tabela a partir do nome do tipo do agregado (ex: "Transaction" → "TB_AUDIT_TRANSACTION").</summary>
    public static string TableName(string aggregateType)
        => $"{Prefix}{aggregateType.ToUpperInvariant()}";
}
