using System.ComponentModel.DataAnnotations;
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
        var traceId = System.Diagnostics.Activity.Current?.Id ?? httpContext.TraceIdentifier;

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

    //private static int GetStatusCode(Exception ex) => ex switch
    //{
    //    DomainValidationException => StatusCodes.Status400BadRequest,
    //    DomainPastEventBookingException => StatusCodes.Status400BadRequest,
    //    DomainUnauthorizedAccessException => StatusCodes.Status401Unauthorized,
    //    DomainNotBookingOwnerException => StatusCodes.Status403Forbidden,
    //    DomainNotFoundException => StatusCodes.Status404NotFound,
    //    DomainAuthorizationException => StatusCodes.Status404NotFound,
    //    DomainNoAvailableSeatsException => StatusCodes.Status409Conflict,
    //    DomainUserAlreadyExistsException => StatusCodes.Status409Conflict,
    //    DomainInvalidStatusTransitionException => StatusCodes.Status409Conflict,
    //    DomainActiveBookingLimitExceededException => StatusCodes.Status409Conflict,
    //    DomainReleaseSeatsException => StatusCodes.Status409Conflict,
    //    OperationCanceledException => StatusCodes.Status499ClientClosedRequest,
    //    _ => StatusCodes.Status400BadRequest 
    //};
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

    //private static string GetTitle(Exception ex) => ex switch
    //{
    //    DomainValidationException => "Validation failed",
    //    DomainPastEventBookingException => "Event already started or passed",
    //    DomainUnauthorizedAccessException => "Authorized access only",
    //    DomainNotBookingOwnerException => "Not have permission",
    //    DomainNotFoundException => "Resource not found",
    //    DomainAuthorizationException => "Wrong authorization",
    //    DomainNoAvailableSeatsException => "No available seats for this event",
    //    DomainUserAlreadyExistsException => "User already exists",
    //    DomainInvalidStatusTransitionException => "Status transition conflict",
    //    DomainActiveBookingLimitExceededException => "Exceeded maximum number of bookings",
    //    DomainReleaseSeatsException => "Failed to release seats",
    //    OperationCanceledException => "The operation was canceled",
    //    _ => "Domain rule violation"
    //};
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
