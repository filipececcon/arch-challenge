using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchChallenge.Dashboard.Tests.Integration.Support;

public static class TestAuth
{
    public const string Scheme = "Test";
}

/// <summary>Autenticação fixa para testes de integração.</summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim("sub", "integration-test-user"),
            new Claim(ClaimTypes.NameIdentifier, "integration-test-user"),
            new Claim("roles", "comerciante"),
        };

        var identity  = new ClaimsIdentity(claims, TestAuth.Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, TestAuth.Scheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
