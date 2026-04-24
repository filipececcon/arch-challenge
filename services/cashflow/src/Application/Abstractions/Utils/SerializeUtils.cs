using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ArchChallenge.CashFlow.Domain.Shared.Projection;

namespace ArchChallenge.CashFlow.Application.Abstractions.Utils;

public static class SerializeUtils
{
    private static readonly HashSet<string> IgnoredEntityJsonPropertyNames = new(
        EntityProjectionRuntimeFields.JsonPropertyNames,
        StringComparer.OrdinalIgnoreCase);

    public static readonly JsonSerializerOptions EntityJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters           = { new JsonStringEnumConverter() },
        TypeInfoResolver     = new DefaultJsonTypeInfoResolver { Modifiers = { StripNotificationPatternFromEntity } }
    };

    private static void StripNotificationPatternFromEntity(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object || !typeof(Entity).IsAssignableFrom(typeInfo.Type))
            return;

        var ignored = typeInfo.Properties
            .Where(p => IgnoredEntityJsonPropertyNames.Contains(p.Name))
            .ToList();

        foreach (var property in ignored)
        {
            typeInfo.Properties.Remove(property);
        }
    }
}