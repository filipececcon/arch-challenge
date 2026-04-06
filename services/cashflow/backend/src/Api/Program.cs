using ArchChallenge.CashFlow.Api.Extensions;
using ArchChallenge.CashFlow.Api.Middlewares;
using ArchChallenge.CashFlow.Application;
using ArchChallenge.CashFlow.Data;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSwaggerConfiguration();
builder.Services.AddLocalizationConfiguration();

builder.Services.AddApplication();

builder.Services.AddData(builder.Configuration);

builder.Services.AddMessaging(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

app.UseLocalizationConfiguration();
app.UseSwaggerConfiguration();

app.UseHttpsRedirection();
app.MapControllers();

if (app.Environment.IsDevelopment())
    await app.MigrateAsync();

await app.RunAsync();

public partial class Program { }
