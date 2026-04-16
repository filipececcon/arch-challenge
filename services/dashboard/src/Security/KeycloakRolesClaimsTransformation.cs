using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace ArchChallenge.Dashboard.Infrastructure.CrossCutting.Security;

/// <summary>
/// Copia as roles de <c>realm_access.roles</c> do JWT do Keycloak
/// para claims <c>roles</c> simples, tornando-as visíveis ao middleware de autorização.
/// </summary>
internal sealed class KeycloakRolesClaimsTransformation : IClaimsTransformation
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
            using var doc = JsonDocument.Parse(realmAccessClaim.Value);
            if (!doc.RootElement.TryGetProperty("roles", out var roles))
                return Task.FromResult(principal);

            foreach (var role in roles.EnumerateArray())
            {
                var name = role.GetString();
                if (name is not null && !identity.HasClaim("roles", name))
                    identity.AddClaim(new Claim("roles", name));
            }
        }
        catch (JsonException) { }

        return Task.FromResult(principal);
    }
}
