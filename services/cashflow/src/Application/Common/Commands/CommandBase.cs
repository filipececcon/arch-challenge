namespace ArchChallenge.CashFlow.Application.Common.Commands;

/// <summary>
/// Propriedades de contexto comuns a comandos: preenchidas por um filtro da API após o model binding
/// (JWT <c>sub</c>, instante da requisição) e propagadas na mensagem até handlers assíncronos.
/// </summary>
/// <remarks>
/// Não herda <see cref="MediatR.IRequest"/> — cada comando declara explicitamente
/// <c>IRequest</c>, <c>IRequest{T}</c> ou <c>IEnqueueCommand{T}</c> para evitar conflito
/// (ex.: <see cref="MediatR.IRequest"/> vs <c>IRequest&lt;EnqueueResult&gt;</c>).
/// </remarks>
public abstract record CommandBase : IAuditable
{
    public string UserId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
