namespace ArchChallenge.CashFlow.Domain.Shared.Projection;

/// <summary>
/// Propriedades herdadas de Flunt / base <c>Entity</c> que não entram em projeções
/// (read model, cache, etc.).
/// </summary>
public static class EntityProjectionRuntimeFields
{
    public static readonly string[] JsonPropertyNames =
    [
        "isValid", "isFailure", "notifications",
        "IsValid", "IsFailure", "Notifications"
    ];
}
