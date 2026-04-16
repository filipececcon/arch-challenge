using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Logging;
using ArchChallenge.CashFlow.Infrastructure.Data.Immutable;
using ArchChallenge.CashFlow.Infrastructure.Data.Relational;
using ArchChallenge.CashFlow.Infrastructure.Outbox.Audit;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddObservability(builder.Configuration);
builder.Services.AddImmutableData(builder.Configuration);
builder.Services.AddRelationalData(builder.Configuration);
builder.Services.AddAuditOutboxWorker(builder.Configuration);

var host = builder.Build();

await host.MigrateAsync();
await host.RunAsync();
