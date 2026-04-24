namespace ArchChallenge.CashFlow.Application.Abstractions.Messaging;

/// <summary>
/// Interface para um barramento de eventos, responsável por publicar mensagens de eventos para outros sistemas
/// ou componentes interessados. O barramento de eventos é uma parte fundamental da arquitetura orientada a eventos,
/// permitindo a comunicação assíncrona entre diferentes partes do sistema. Ele pode ser implementado usando diversos
/// mecanismos de transporte, como filas de mensagens, tópicos de publicação/assinatura ou sistemas de mensagens
/// baseados em nuvem. O objetivo principal do barramento de eventos é desacopar os produtores de eventos dos
/// consumidores, promovendo uma arquitetura mais flexível e escalável. Ele é usado para publicar eventos que
/// representam mudanças de estado ou ações significativas dentro do sistema, permitindo que outros componentes
/// reajam a esses eventos de forma assíncrona e independente.  
/// </summary>
public interface IEventBus
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
    Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken = default);
}
