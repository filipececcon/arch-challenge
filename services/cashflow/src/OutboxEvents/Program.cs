using ArchChallenge.CashFlow.Infrastructure.Agents.Outbox.Events;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Logging;
using ArchChallenge.CashFlow.Infrastructure.Data.Documents;
using ArchChallenge.CashFlow.Infrastructure.Data.Relational;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddObservability(builder.Configuration);
builder.Services.AddRelationalData(builder.Configuration);
builder.Services.AddDocumentsData(builder.Configuration);
builder.Services.AddEventsOutboxWorker(builder.Configuration);

var host = builder.Build();

await host.MigrateAsync();
await host.RunAsync();
