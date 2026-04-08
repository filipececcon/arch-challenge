using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Caching;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId());

builder.Services.AddControllers();

builder.Services.AddSwaggerConfiguration();
builder.Services.AddLocalizationConfiguration();

builder.Services.AddCaching(builder.Configuration);

builder.Services.AddApplication();

builder.Services.AddData(builder.Configuration);

builder.Services.AddMessaging(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

app.UseLocalizationConfiguration();
app.UseSwaggerConfiguration();

app.UseHttpsRedirection();
app.UseHttpMetrics();
app.MapControllers();
app.MapMetrics();

if (app.Environment.IsDevelopment())
    await app.MigrateAsync();

await app.RunAsync();

public partial class Program { }
