using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using CoreEvents.Shared.Contracts.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Users.Api.ExceptionHandlers;

public class DomainExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<DomainExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not AppException && exception is not OperationCanceledException)
        {
            return false;
        }

        var statusCode = GetStatusCode(exception);
        Activity.Current?.AddException(exception);
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        logger.LogWarning("Request error: {Message} at {Path}. TraceId: {TraceId}",
            exception.Message, httpContext.Request.Path, traceId);

        var problem = new ProblemDetails
        {
            Title = GetTitle(exception),
            Status = statusCode,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        if (exception is AppException appException)
        {
            problem.Extensions["errorCode"] = appException.ErrorCode;

            if (appException.ErrorData != null)
            {
                problem.Extensions["errorData"] = appException.ErrorData;
            }

            if (appException.ValidationErrors != null)
            {
                problem.Extensions["errors"] = appException.ValidationErrors;
            }
        }

        httpContext.Response.StatusCode = statusCode;

        var successfullyWrote = await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });

        if (!successfullyWrote)
        {
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        }

        return true;
    }

    private static int GetStatusCode(Exception ex) => ex switch
    {
        ValidationException => StatusCodes.Status400BadRequest, // Базовый для ошибок валидации
        UnauthorizedException => StatusCodes.Status401Unauthorized, // Базовый для авторизации
        ForbiddenException => StatusCodes.Status403Forbidden, // Базовый для прав доступа
        NotFoundException => StatusCodes.Status404NotFound, // Базовый для "не найдено"
        ConflictException => StatusCodes.Status409Conflict, // Базовый для конфликтов (лимиты, дубликаты, статусы)
        AppException => StatusCodes.Status400BadRequest, // Фолбэк для остальных бизнес-ошибок
        OperationCanceledException => StatusCodes.Status499ClientClosedRequest,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string GetTitle(Exception ex) => ex switch
    {
        ValidationException => "Validation failed",
        UnauthorizedException => "Authorized access only",
        ForbiddenException => "Not have permission",
        NotFoundException => "Resource not found",
        ConflictException => "State conflict", // Универсальный заголовок для 409
        AppException => "Domain rule violation",
        OperationCanceledException => "The operation was canceled",
        _ => "An error occurred"
    };
}
