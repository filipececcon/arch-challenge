using ArchChallenge.CashFlow.Infrastructure.Agents.Outbox.Configuration;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Logging;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging;
using ArchChallenge.CashFlow.Infrastructure.Data.Documents;
using ArchChallenge.CashFlow.Infrastructure.Data.Immutable;
using ArchChallenge.CashFlow.Infrastructure.Data.Relational;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddObservability(builder.Configuration);
builder.Services.AddRelationalData(builder.Configuration);
builder.Services.AddDocumentsData(builder.Configuration);
builder.Services.AddMessagingPublisher(builder.Configuration);
builder.Services.AddImmutableData(builder.Configuration);
builder.Services.AddOutboxAgent(builder.Configuration);

var host = builder.Build();

await host.MigrateAsync();
await host.RunAsync();
