using System.Security.Claims;

namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Security;

/// <summary>
/// Resolve o identificador do usuário a partir do principal (JWT <c>sub</c> ou <see cref="ClaimTypes.NameIdentifier"/>).
/// </summary>
public static class UserIdentity
{
    public static string ResolveUserId(ClaimsPrincipal user) =>
        user.FindFirstValue("sub")
        ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? string.Empty;
}
