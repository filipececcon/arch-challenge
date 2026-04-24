using ArchChallenge.CashFlow.Application.Abstractions.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ArchChallenge.CashFlow.Api.Extensions;

public static class ResultHttpExtensions
{
    /// <summary>Projeta o envelope <see cref="Result{T}"/> em <see cref="IActionResult"/>. 204 nunca envia corpo; demais códigos serializam o envelope.</summary>
    public static IActionResult ToActionResult<T>(this Result<T> result) where T : class
    {
        if (result.IsSuccess && result.StatusCode == StatusCodes.Status204NoContent)
            return new NoContentResult();

        return new ObjectResult(result) { StatusCode = result.StatusCode };
    }
}
