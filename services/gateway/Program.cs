using ArchChallenge.Gateway.Authorization;
using ArchChallenge.Gateway.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MMLib.SwaggerForOcelot.DependencyInjection;
using Ocelot.Authorization;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

var useLocalDownstreamHosts = builder.Configuration.GetValue("Gateway:UseLocalDownstreamHosts", false);
var ocelotFile = useLocalDownstreamHosts ? "ocelot.Development.json" : "ocelot.json";

builder.Configuration.AddJsonFile(ocelotFile, optional: false, reloadOnChange: true);

builder.Services.AddSingleton<IClaimsAuthorizer, CommaSeparatedRolesClaimsAuthorizer>();
builder.Services.AddSingleton<IClaimsTransformation, KeycloakRolesClaimsTransformation>();

var keycloakSection = builder.Configuration.GetSection("Keycloak");
var authority = keycloakSection["Authority"]
    ?? throw new InvalidOperationException("Configuração obrigatória: Keycloak:Authority");
var validAudiences = keycloakSection.GetSection("ValidAudiences").Get<string[]>()
    ?? ["cashflow-api", "dashboard-api", "account"];
var validIssuers = keycloakSection.GetSection("ValidIssuers").Get<string[]>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = authority;
        options.RequireHttpsMetadata = keycloakSection.GetValue("RequireHttpsMetadata", false);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            AudienceValidator = (audiences, _, _) =>
            {
                var list = audiences?.ToList() ?? [];
                return list.Exists(validAudiences.Contains);
            },
        };

        if (validIssuers is { Length: > 0 })
            options.TokenValidationParameters.ValidIssuers = validIssuers;

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var transformation = context.HttpContext.RequestServices.GetRequiredService<IClaimsTransformation>();
                context.Principal = await transformation.TransformAsync(context.Principal!);
            },
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:4200"];
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOcelot(builder.Configuration);
builder.Services.AddSwaggerForOcelot(builder.Configuration);

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerForOcelotUI(o => o.PathToSwaggerGenerator = "/swagger/docs");
}

await app.UseOcelot();
await app.RunAsync();
