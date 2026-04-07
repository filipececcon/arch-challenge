using FluentValidation;
using System.Net;
using System.Text.Json;
using ArchChallenge.CashFlow.Domain.Shared.Exceptions;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.I18n;
using Microsoft.Extensions.Localization;

namespace ArchChallenge.CashFlow.Api.Middlewares;

public class ExceptionMiddleware(
    RequestDelegate next,
    ILogger<ExceptionMiddleware> logger,
    IStringLocalizer<Messages> localizer)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning("Validation error: {Errors}", ex.Errors);
            await WriteResponseAsync(context, HttpStatusCode.BadRequest, new
            {
                type = "ValidationError",
                message = localizer[MessageKeys.Exception.ValidationError].Value,
                errors = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            });
        }
        catch (DomainException ex)
        {
            logger.LogWarning("Domain error: {Message}", ex.Message);
            await WriteResponseAsync(context, HttpStatusCode.BadRequest, new
            {
                type = "DomainError",
                message = localizer[MessageKeys.Exception.DomainError].Value,
                detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error.");
            await WriteResponseAsync(context, HttpStatusCode.InternalServerError, new
            {
                type = "InternalError",
                message = localizer[MessageKeys.Exception.InternalError].Value
            });
        }
    }

    private static async Task WriteResponseAsync(HttpContext context, HttpStatusCode statusCode, object body)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
