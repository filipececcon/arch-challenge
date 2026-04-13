namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging.Common.Attributes;

/// <summary>
/// ConsumerDefinition genérico que lê a configuração do endpoint a partir de qualquer atributo
/// que implemente <see cref="IConsumerEndpointMetadata"/> declarado na classe do consumer.
/// Registrado automaticamente pelo DI — consumidores não precisam criar suas próprias definitions.
/// </summary>
public sealed class AttributeConsumerDefinition<TConsumer> : ConsumerDefinition<TConsumer>
    where TConsumer : class, IConsumer
{
    public AttributeConsumerDefinition()
    {
        EndpointName = GetMetadata().EndpointName;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<TConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.ConfigureConsumeTopology = GetMetadata().ConfigureConsumeTopology;
    }

    private static IConsumerEndpointMetadata GetMetadata()
        => typeof(TConsumer).GetCustomAttributes(inherit: false)
               .OfType<IConsumerEndpointMetadata>()
               .FirstOrDefault()
           ?? throw new InvalidOperationException(
               $"Consumer '{typeof(TConsumer).Name}' must have the [ConsumerChannel<TChannel>] attribute.");
}
