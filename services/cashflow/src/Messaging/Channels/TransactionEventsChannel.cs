using RabbitMQ.Client;

namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging.Channels;

/// <summary>
/// Canal de evento: notifica outros serviços (ex: dashboard) que uma transação foi concluída.
///
/// Exchange do tipo Topic para permitir que consumidores filtrem por routing key.
/// Não há consumer local neste serviço — apenas publicação.
/// </summary>
public sealed class TransactionEventsChannel : IChannel
{
    public string Name => "cashflow.events";

    public void Configure(IRabbitMqBusFactoryConfigurator cfg)
    {
        cfg.Message<TransactionProcessedEvent>(m => m.SetEntityName(Name));
        cfg.Publish<TransactionProcessedEvent>(p => p.ExchangeType = ExchangeType.Topic);
    }
}
