using System.Reflection;
using Microsoft.Extensions.Options;

namespace ArchChallenge.CashFlow.Infrastructure.Data.Outbox;

/// <summary>
/// Valida no startup que todos os eventos de domínio concretos (subclasses de <see cref="DomainEvent"/>)
/// possuem entrada correspondente em <see cref="OutboxWorkerOptions.CollectionMap"/>.
///
/// Garante que nenhum evento chegue ao <see cref="OutboxWorkerService"/> sem ter uma
/// coleção MongoDB mapeada, evitando descarte silencioso de eventos por falta de configuração.
///
/// A descoberta usa a convenção de nomenclatura do <see cref="DomainEvent"/>:
/// <c>GetType().Name.Replace("Event", "")</c>.
/// </summary>
internal sealed class OutboxWorkerOptionsValidator : IValidateOptions<OutboxWorkerOptions>
{
    public ValidateOptionsResult Validate(string? name, OutboxWorkerOptions options)
    {
        var missing = DiscoverDomainEventNames()
            .Where(eventName => !options.CollectionMap.ContainsKey(eventName))
            .Order()
            .ToList();

        return missing.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                $"OutboxWorker.CollectionMap is missing entries for: {string.Join(", ", missing)}. " +
                 "Add the EventType → MongoDB collection mapping in appsettings.json.");
    }

    private static IEnumerable<string> DiscoverDomainEventNames() =>
        AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try   { return a.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { return ex.Types.OfType<Type>(); }
            })
            .Where(t => t is { IsAbstract: false, IsClass: true }
                     && typeof(DomainEvent).IsAssignableFrom(t))
            .Select(t => t.Name.Replace("Event", string.Empty));
}
