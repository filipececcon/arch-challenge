using ArchChallenge.Dashboard.Api.Extensions;
using ArchChallenge.Dashboard.Api.Middlewares;
using ArchChallenge.Dashboard.Application;
using ArchChallenge.Dashboard.Infrastructure.CrossCutting.Logging;
using ArchChallenge.Dashboard.Infrastructure.CrossCutting.Messaging.DependencyInjection;
using ArchChallenge.Dashboard.Infrastructure.CrossCutting.Security;
using ArchChallenge.Dashboard.Infrastructure.Data;
using Prometheus;

Serilog.Debugging.SelfLog.Enable(msg => Console.Error.WriteLine($"[SERILOG INTERNAL] {msg}"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddObservability(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddSwaggerConfiguration();
builder.Services.AddSecurityConfiguration(builder.Configuration);
builder.Services.AddHealthChecksConfiguration(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddData(builder.Configuration);
builder.Services.AddMessaging(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseSwaggerConfiguration();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpMetrics();
app.MapControllers();
app.MapMetrics();
app.MapHealthCheckEndpoints();

await app.RunAsync();

public partial class Program { }
