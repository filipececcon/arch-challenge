namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging.Common.Attributes;

/// <summary>
/// Vincula um consumer ao canal que ele consome.
///
/// O nome da fila e a política de topologia são derivados de <typeparamref name="TChannel"/>,
/// eliminando strings duplicadas entre a definição do canal e o consumer.
///
/// Exemplo:
/// <code>
/// [ConsumerChannel&lt;TransactionCreateChannel&gt;]
/// public sealed class ExecuteTransactionConsumer : CommandConsumerBase&lt;...&gt; { }
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ConsumerChannelAttribute<TChannel> : Attribute, IConsumerEndpointMetadata
    where TChannel : IChannel, new()
{
    private static readonly IChannel Channel = new TChannel();

    public string EndpointName => Channel.Name;
    public bool ConfigureConsumeTopology => Channel.ConfigureConsumeTopology;
}
