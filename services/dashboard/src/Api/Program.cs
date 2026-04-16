using ArchChallenge.Dashboard.Api.Extensions;
using ArchChallenge.Dashboard.Application;
using ArchChallenge.Dashboard.Data;
using ArchChallenge.Dashboard.Messaging.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSwaggerConfiguration();
builder.Services.AddApplication();
builder.Services.AddData(builder.Configuration);
builder.Services.AddMessaging(builder.Configuration);

var app = builder.Build();

app.UseSwaggerConfiguration();

app.UseHttpsRedirection();
app.MapControllers();

await app.RunAsync();

public partial class Program { }
