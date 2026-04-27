using System.Security.Claims;
using ArchChallenge.CashFlow.Domain.Shared.Audit;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Security.Filters;

/// <summary>
/// Preenche <see cref="CommandBase.UserId"/> (claim <c>sub</c> ou <see cref="ClaimTypes.NameIdentifier"/>)
/// e <see cref="CommandBase.OccurredAt"/> após o model binding.
/// </summary>
public sealed class IdentityCommandFilter : IAsyncActionFilter
{
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var principal = context.HttpContext.User;
        
        var userId= UserIdentity.ResolveUserId(principal);

        var occurredAt = DateTime.UtcNow;

        foreach (var arg in context.ActionArguments.Values.OfType<IAuditable>())
        {
            arg.UserId     = userId;
            
            arg.OccurredAt = occurredAt;
        }

        return next();
    }
}
