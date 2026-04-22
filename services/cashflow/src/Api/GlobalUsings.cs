global using MediatR;
global using Prometheus;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Mvc;
global using ArchChallenge.CashFlow.Api;
global using ArchChallenge.CashFlow.Api.Extensions;
global using ArchChallenge.CashFlow.Api.Middlewares;
global using ArchChallenge.CashFlow.Application;

global using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Caching;
global using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Logging;
global using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging;
global using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Security;
global using ArchChallenge.CashFlow.Infrastructure.Data.Relational;
global using ArchChallenge.CashFlow.Infrastructure.Data.Documents;

