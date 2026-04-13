namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging.Common.Attributes;

/// <summary>
/// Contrato interno que permite ao <see cref="AttributeConsumerDefinition{TConsumer}"/>
/// ler as configurações de endpoint sem precisar conhecer o tipo concreto do atributo.
/// </summary>
internal interface IConsumerEndpointMetadata
{
    string EndpointName { get; }
    bool ConfigureConsumeTopology { get; }
}
