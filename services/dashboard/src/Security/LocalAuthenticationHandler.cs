using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchChallenge.Dashboard.Infrastructure.CrossCutting.Security;

/// <summary>
/// Auto-autentica toda requisição com um usuário fictício para testes locais.
/// NUNCA deve ser registrado em ambientes produtivos.
/// </summary>
internal sealed class LocalAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Local";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "local-dev-user"),
            new Claim(ClaimTypes.Name, "Local User"),
            new Claim("realm_access", """{"roles":["comerciante","admin"]}"""),
            new Claim("roles", "comerciante"),
            new Claim("roles", "admin"),
        };

        var identity  = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
