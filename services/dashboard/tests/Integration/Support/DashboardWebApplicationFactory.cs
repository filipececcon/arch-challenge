using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ArchChallenge.Dashboard.Tests.Integration.Support;

public sealed class DashboardWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(static services =>
        {
            services.PostConfigure<AuthenticationOptions>(o =>
            {
                o.DefaultAuthenticateScheme = TestAuth.Scheme;
                o.DefaultChallengeScheme    = TestAuth.Scheme;
            });

            services
                .AddAuthentication(TestAuth.Scheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuth.Scheme, _ => { });
        });
    }
}
