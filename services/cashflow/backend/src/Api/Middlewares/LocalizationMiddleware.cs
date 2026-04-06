using System.Globalization;

namespace ArchChallenge.CashFlow.Api.Middlewares;

public class LocalizationMiddleware(RequestDelegate next)
{
    private static readonly string[] SupportedCultures = ["en-US", "pt-BR"];
    private const string DefaultCulture = "en-US";

    public async Task InvokeAsync(HttpContext context)
    {
        var culture = ResolveFromHeader(context.Request.Headers.AcceptLanguage.ToString())
                      ?? DefaultCulture;

        var cultureInfo = new CultureInfo(culture);
        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;

        await next(context);
    }

    private static string? ResolveFromHeader(string? acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage))
            return null;

        // Parse "Accept-Language: pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7"
        var candidates = acceptLanguage
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(entry =>
            {
                var parts = entry.Trim().Split(';');
                var tag = parts[0].Trim();
                var quality = 1.0;

                if (parts.Length > 1)
                {
                    var qPart = parts[1].Trim();
                    if (qPart.StartsWith("q=", StringComparison.OrdinalIgnoreCase))
                        double.TryParse(qPart[2..], NumberStyles.Any, CultureInfo.InvariantCulture, out quality);
                }

                return (tag, quality);
            })
            .OrderByDescending(x => x.quality);

        foreach (var (tag, _) in candidates)
        {
            // Exact match first (e.g. "pt-BR")
            var exact = SupportedCultures.FirstOrDefault(c =>
                c.Equals(tag, StringComparison.OrdinalIgnoreCase));
            if (exact is not null) return exact;

            // Partial match by primary language subtag (e.g. "pt" → "pt-BR")
            var primary = tag.Split('-')[0];
            var partial = SupportedCultures.FirstOrDefault(c =>
                c.StartsWith(primary + "-", StringComparison.OrdinalIgnoreCase));
            if (partial is not null) return partial;
        }

        return null;
    }
}

