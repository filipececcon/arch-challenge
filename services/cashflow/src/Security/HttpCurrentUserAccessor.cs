using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Security;

internal sealed class HttpCurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    public string UserId =>
        UserIdentity.ResolveUserId(httpContextAccessor.HttpContext?.User!);
}
