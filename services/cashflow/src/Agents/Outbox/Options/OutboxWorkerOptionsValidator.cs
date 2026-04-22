using System.Reflection;
using ArchChallenge.CashFlow.Infrastructure.Agents.Outbox.Workers;
using Microsoft.Extensions.Options;

namespace ArchChallenge.CashFlow.Infrastructure.Agents.Outbox.Options;

/// <summary>
/// Valida no startup que todos os eventos de aplicação concretos (tipos concretos cujo nome
/// termina em "OutboxEvent" no namespace <c>ArchChallenge.CashFlow.Application</c>) possuem entrada
/// correspondente em <see cref="OutboxWorkerOptions.CollectionMap"/>.
///
/// Garante que nenhum evento chegue ao <see cref="MongoOutboxWorkerService"/> sem ter uma
/// coleção MongoDB mapeada, evitando descarte silencioso de eventos por falta de configuração.
///
/// A descoberta usa a convenção de nomenclatura: <c>TypeName.Replace("OutboxEvent", "")</c>.
/// </summary>
internal sealed class OutboxWorkerOptionsValidator : IValidateOptions<OutboxWorkerOptions>
{
    public ValidateOptionsResult Validate(string? name, OutboxWorkerOptions options)
    {
        var missing = DiscoverEventNames()
            .Where(eventName => !options.CollectionMap.ContainsKey(eventName))
            .Order()
            .ToList();

        return missing.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                $"OutboxWorker.CollectionMap is missing entries for: {string.Join(", ", missing)}. " +
                 "Add the EventType → MongoDB collection mapping in appsettings.json.");
    }

    private static IEnumerable<string> DiscoverEventNames() =>
        AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try   { return a.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { return ex.Types.OfType<Type>(); }
            })
            .Where(t => t is { IsAbstract: false, IsClass: true }
                     && t.Name.EndsWith("OutboxEvent", StringComparison.Ordinal)
                     && t.Namespace?.StartsWith("ArchChallenge.CashFlow.Application") == true)
            .Select(t => t.Name.Replace("OutboxEvent", string.Empty));
}
