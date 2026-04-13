namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging.Channels;

/// <summary>
/// Descreve um canal de mensageria: nome do exchange, tipo e política de topologia do consumer.
/// Implementações são descobertas automaticamente e aplicadas na configuração do MassTransit.
///
/// Regra de ouro: cada mensagem tem exatamente um canal. O canal é a fonte única de
/// verdade para o nome do exchange/fila — publisher e consumer referenciam o mesmo canal.
/// </summary>
public interface IChannel
{
    /// <summary>Nome do exchange RabbitMQ (e da fila, para canais de comando).</summary>
    string Name { get; }

    /// <summary>
    /// Controla se o MassTransit deve criar bindings de topologia baseados no tipo da mensagem.
    /// <c>false</c> (padrão) é o correto para canais com nome personalizado: o MassTransit
    /// criará apenas a fila com o mesmo nome do endpoint, sem bindings extras de FQN.
    /// </summary>
    bool ConfigureConsumeTopology => false;

    void Configure(IRabbitMqBusFactoryConfigurator cfg);
}
