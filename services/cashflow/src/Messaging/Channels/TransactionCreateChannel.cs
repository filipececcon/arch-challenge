using ArchChallenge.CashFlow.Application.Transactions.Commands.EnqueueTransaction;

namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging.Channels;

/// <summary>
/// Canal de comando: recebe a solicitação de criação de transação e a entrega ao worker assíncrono.
///
/// Fluxo:
///   POST /transactions → EnqueueTransactionMessage publicada neste exchange
///   → ExecuteTransactionConsumer lê da fila deste canal e executa o comando de negócio.
///
/// Exchange e fila compartilham o mesmo nome. Como o nome é personalizado (não FQN),
/// <see cref="IChannel.ConfigureConsumeTopology"/> permanece <c>false</c> (padrão).
/// </summary>
public sealed class TransactionCreateChannel : IChannel
{
    public string Name => "cashflow.transaction.create";

    public void Configure(IRabbitMqBusFactoryConfigurator cfg)
        => cfg.Message<EnqueueTransactionMessage>(m => m.SetEntityName(Name));
}
