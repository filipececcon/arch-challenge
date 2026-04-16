using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ArchChallenge.Dashboard.Infrastructure.CrossCutting.Security;

public static class DependencyInjection
{
    public static IServiceCollection AddSecurityConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        if (configuration.GetValue<bool>("Security:Disabled"))
        {
            services
                .AddAuthentication(LocalAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, LocalAuthenticationHandler>(
                    LocalAuthenticationHandler.SchemeName, _ => { });
        }
        else
        {
            var keycloak  = configuration.GetSection("Keycloak");
            var authority = keycloak["Authority"]
                ?? throw new InvalidOperationException("Configuração ausente: Keycloak:Authority");
            var audience  = keycloak["Audience"] ?? "dashboard-api";

            services.AddSingleton<IClaimsTransformation, KeycloakRolesClaimsTransformation>();

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority             = authority;
                    options.RequireHttpsMetadata  = keycloak.GetValue("RequireHttpsMetadata", false);
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = true,
                        ValidAudience    = audience,
                        ValidateIssuer   = true,
                        ValidateLifetime = true,
                        ClockSkew        = TimeSpan.FromSeconds(30),
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async ctx =>
                        {
                            var transformer = ctx.HttpContext.RequestServices
                                .GetRequiredService<IClaimsTransformation>();
                            ctx.Principal = await transformer.TransformAsync(ctx.Principal!);
                        }
                    };
                });
        }

        services.AddAuthorization();
        return services;
    }
}
