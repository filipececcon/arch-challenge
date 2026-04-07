using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace ArchChallenge.Gateway.Security;

/// <summary>
/// Copia as roles de <c>realm_access.roles</c> para claims <c>roles</c> de valor simples,
/// para uso com <c>RouteClaimsRequirement</c> do Ocelot.
/// </summary>
public sealed class KeycloakRolesClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity)
            return Task.FromResult(principal);

        var realmAccessClaim = identity.FindFirst("realm_access");
        if (realmAccessClaim is null)
            return Task.FromResult(principal);

        try
        {
            using var realmAccess = JsonDocument.Parse(realmAccessClaim.Value);
            if (!realmAccess.RootElement.TryGetProperty("roles", out var roles))
                return Task.FromResult(principal);

            foreach (var role in roles.EnumerateArray())
            {
                var roleName = role.GetString();
                if (roleName is null || identity.HasClaim("roles", roleName))
                    continue;

                identity.AddClaim(new Claim("roles", roleName));
            }
        }
        catch (JsonException)
        {
            return Task.FromResult(principal);
        }

        return Task.FromResult(principal);
    }
}
