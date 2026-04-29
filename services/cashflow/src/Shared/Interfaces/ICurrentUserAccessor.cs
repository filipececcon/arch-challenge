namespace ArchChallenge.CashFlow.Domain.Shared.Interfaces;

/// <summary>
/// Abstração para resolução do usuário corrente.
/// Desacopla a camada Application de dependências web (IHttpContextAccessor).
/// </summary>
public interface ICurrentUserAccessor
{
    string UserId { get; }
}
