using System.Text.Json;
using System.Text.Json.Nodes;

namespace ArchChallenge.CashFlow.Domain.Shared.Projection;

public static class EntityProjectionJson
{
    /// <summary>
    /// Remove do objeto JSON as propriedades só de runtime (Flunt / base Entity) e
    /// normaliza nomes de propriedades para camelCase (incluindo objetos aninhados).
    /// </summary>
    public static JsonElement RemoveRuntimeFields(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) return EnsureCamelCase(element);

        var node = JsonNode.Parse(element.GetRawText());

        if (node is not JsonObject obj) return EnsureCamelCase(element);

        foreach (var name in EntityProjectionRuntimeFields.JsonPropertyNames)
        {
            obj.Remove(name);
        }

        return EnsureCamelCase(JsonSerializer.SerializeToElement(obj));
    }

    /// <summary>
    /// Garante camelCase em todas as chaves de objetos (recursivo em objetos e arrays).
    /// </summary>
    private static JsonElement EnsureCamelCase(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ToCamelCaseObject(element),
            JsonValueKind.Array  => ToCamelCaseArray(element),
            _                    => element
        };
    }

    /// <summary>
    /// Converte um array JSON para camelCase (recursivo em objetos e arrays aninhados).
    /// </summary>
    /// <param name="array"></param>
    /// <returns></returns>
    private static JsonElement ToCamelCaseArray(JsonElement array)
    {
        var arr = new JsonArray();
        foreach (var item in array.EnumerateArray())
            arr.Add(JsonNode.Parse(EnsureCamelCase(item).GetRawText()));

        return JsonSerializer.SerializeToElement(arr);
    }

    /// <summary>
    /// Converte um objeto JSON para camelCase (recursivo em objetos e arrays aninhados).
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    private static JsonElement ToCamelCaseObject(JsonElement obj)
    {
        var jsonObject = new JsonObject();
        foreach (var prop in obj.EnumerateObject())
        {
            var camelName = JsonNamingPolicy.CamelCase.ConvertName(prop.Name);
            jsonObject[camelName] = JsonNode.Parse(EnsureCamelCase(prop.Value).GetRawText());
        }

        return JsonSerializer.SerializeToElement(jsonObject);
    }
}
