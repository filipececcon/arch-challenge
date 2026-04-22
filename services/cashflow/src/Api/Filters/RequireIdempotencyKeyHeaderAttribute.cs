using Microsoft.AspNetCore.Mvc.Filters;

namespace ArchChallenge.CashFlow.Api.Filters;

/// <summary>
/// Valida a presença do header <c>Idempotency-Key</c> (pode ser vazio para nova operação) e, quando não vazio,
/// exige UUID. Armazena o resultado em <see cref="HttpContext.Items"/> para leitura via <see cref="IdempotencyKeyHttpContextExtensions.GetIdempotencyKey"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class RequireIdempotencyKeyHeaderAttribute : ActionFilterAttribute
{
    public const string HttpContextItemsKey = nameof(RequireIdempotencyKeyHeaderAttribute) + ".ParsedKey";

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        
        if (!request.Headers.TryGetValue("Idempotency-Key", out var value))
        {
            context.Result = new BadRequestObjectResult(
                new { error = "Header Idempotency-Key is required." });
            return;
        }

        var raw = value.ToString();
        
        Guid? idempotencyKey = null;
        
        if (!string.IsNullOrWhiteSpace(raw))
        {
            if (!Guid.TryParse(raw, out var parsed))
            {
                context.Result = new BadRequestObjectResult(
                    new { error = "Idempotency-Key must be a UUID when not empty." });
                return;
            }

            idempotencyKey = parsed;
        }

        var holder = new IdempotencyKeyHolder(idempotencyKey);
        
        context.HttpContext.Items[HttpContextItemsKey] = holder;
        
        await next();
    }
}

/// <summary>Envelope para guardar uma chave <see cref="Guid"/> opcional em <see cref="HttpContext.Items"/> (evita ambiguidade com ausência de chave).</summary>
internal readonly record struct IdempotencyKeyHolder(Guid? Key);

public static class IdempotencyKeyHttpContextExtensions
{
    /// <summary>
    /// Retorna a chave de idempotência preenchida pelo <see cref="RequireIdempotencyKeyHeaderAttribute"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Se o atributo não tiver executado com sucesso nesta requisição.</exception>
    public static Guid? GetIdempotencyKey(this HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(RequireIdempotencyKeyHeaderAttribute.HttpContextItemsKey, out var o)
            && o is IdempotencyKeyHolder holder)
        {
            return holder.Key;
        }

        throw new InvalidOperationException(
            "Idempotency key is unavailable. Apply [RequireIdempotencyKeyHeader] to the action.");
    }
}
