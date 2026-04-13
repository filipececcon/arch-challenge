using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Caching;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Logging;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Security;

Serilog.Debugging.SelfLog.Enable(msg => Console.Error.WriteLine($"[SERILOG INTERNAL] {msg}"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddObservability(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddSwaggerConfiguration();
builder.Services.AddLocalizationConfiguration();
builder.Services.AddSecurityConfiguration(builder.Configuration);
builder.Services.AddCaching(builder.Configuration);
builder.Services.AddHealthChecksConfiguration(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddData(builder.Configuration);
builder.Services.AddMessaging(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

app.UseLocalizationConfiguration();
app.UseSwaggerConfiguration();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpMetrics();
app.MapControllers();
app.MapMetrics();
app.MapHealthCheckEndpoints();

await app.MigrateAsync();

await app.RunAsync();

namespace ArchChallenge.CashFlow.Api
{
    public partial class Program { }
}
