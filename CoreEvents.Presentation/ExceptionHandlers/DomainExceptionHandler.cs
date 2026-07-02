using CoreEvents.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CoreEvents.Presentation.ExceptionHandlers
{
    public class DomainExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<DomainExceptionHandler> logger)
        : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not DomainException && exception is not OperationCanceledException)
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

            if (exception is DomainException domainEx)
            {
                problem.Extensions["errorCode"] = domainEx.ErrorCode;

                if (domainEx is DomainNotFoundException notFoundEx)
                {
                    problem.Extensions["errorData"] = new
                    {
                        parameter = notFoundEx.ParamName,
                        value = notFoundEx.Key
                    };
                }
                else if (domainEx is DomainValidationException validationEx)
                {
                    problem.Extensions["errors"] = validationEx.Errors;
                }
            }

            httpContext.Response.StatusCode = statusCode;

            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problem
            });

            return true;
        }

        private static int GetStatusCode(Exception ex) => ex switch
        {
            DomainValidationException => StatusCodes.Status400BadRequest,
            DomainNotFoundException => StatusCodes.Status404NotFound,
            DomainNoAvailableSeatsException => StatusCodes.Status409Conflict,
            OperationCanceledException => StatusCodes.Status499ClientClosedRequest,
            _ => StatusCodes.Status400BadRequest 
        };

        private static string GetTitle(Exception ex) => ex switch
        {
            DomainValidationException => "Validation failed",
            DomainNotFoundException => "Resource not found",
            DomainNoAvailableSeatsException => "No available seats for this event",
            OperationCanceledException => "The operation was canceled",
            _ => "Domain rule violation"
        };
    }
}
