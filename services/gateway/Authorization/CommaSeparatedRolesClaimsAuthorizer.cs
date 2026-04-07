using Ocelot.Authorization;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Infrastructure.Claims;
using Ocelot.Responses;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace ArchChallenge.Gateway.Authorization;

/// <summary>
/// Autorizador compatível com <c>RouteClaimsRequirement</c> no formato <c>"roles": "comerciante,admin"</c>:
/// o usuário precisa possuir <b>ao menos uma</b> das roles listadas (separadas por vírgula).
/// O comportamento padrão do Ocelot exige correspondência exata de um único valor.
/// </summary>
public sealed class CommaSeparatedRolesClaimsAuthorizer : IClaimsAuthorizer
{
    private static readonly Regex DynamicClaimRegex = new(@"^\{(?<name>[^}]+)\}$", RegexOptions.Compiled);

    private readonly IClaimsParser _claimsParser;

    public CommaSeparatedRolesClaimsAuthorizer(IClaimsParser claimsParser)
    {
        _claimsParser = claimsParser;
    }

    public Response<bool> Authorize(
        ClaimsPrincipal claimsPrincipal,
        Dictionary<string, string> routeClaimsRequirement,
        List<PlaceholderNameAndValue> urlPathPlaceholderNameAndValues)
    {
        foreach (var required in routeClaimsRequirement)
        {
            var values = _claimsParser.GetValuesByClaimType(claimsPrincipal.Claims, required.Key);

            if (values.IsError)
                return new ErrorResponse<bool>(values.Errors);

            if (values.Data is null)
                return new ErrorResponse<bool>(new UserDoesNotHaveClaimError($"user does not have claim {required.Key}"));

            var match = DynamicClaimRegex.Match(required.Value);
            if (match.Success)
            {
                var variableName = match.Groups["name"].Value;
                var matchingPlaceholders = urlPathPlaceholderNameAndValues
                    .Where(p => p.Name.Equals(variableName, StringComparison.Ordinal))
                    .Take(2)
                    .ToArray();

                if (matchingPlaceholders.Length == 1)
                {
                    var actualValue = matchingPlaceholders[0].Value;
                    var authorized = values.Data.Contains(actualValue);
                    if (!authorized)
                    {
                        return new ErrorResponse<bool>(new ClaimValueNotAuthorizedError(
                            $"dynamic claim value for {variableName} of {string.Join(", ", values.Data)} is not the same as required value: {actualValue}"));
                    }
                }
                else if (matchingPlaceholders.Length == 0)
                {
                    return new ErrorResponse<bool>(new ClaimValueNotAuthorizedError(
                        $"config error: requires variable claim value: {variableName} placeholders does not contain that variable: {string.Join(", ", urlPathPlaceholderNameAndValues.Select(p => p.Name))}"));
                }
                else
                {
                    return new ErrorResponse<bool>(new ClaimValueNotAuthorizedError(
                        $"config error: requires variable claim value: {required.Value} but placeholders are ambiguous: {string.Join(", ", urlPathPlaceholderNameAndValues.Where(p => p.Name.Equals(variableName, StringComparison.Ordinal)).Select(p => p.Value))}"));
                }
            }
            else
            {
                var requiredAlternatives = required.Value
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                var authorized = requiredAlternatives.Any(alt => values.Data.Contains(alt));
                if (!authorized)
                {
                    return new ErrorResponse<bool>(new ClaimValueNotAuthorizedError(
                        $"claim value: {string.Join(", ", values.Data)} is not the same as required value: {required.Value} for type: {required.Key}"));
                }
            }
        }

        return new OkResponse<bool>(true);
    }
}
