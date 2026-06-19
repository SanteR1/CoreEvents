using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace CoreEvents.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            IProblemDetailsService problemDetailsService,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _problemDetailsService = problemDetailsService;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var statusCode = GetStatusCode(ex);
                var traceId = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;

                // Если это ошибка клиента (400, 404), логируем коротко как Warning
                if (statusCode < 500)
                {
                    _logger.LogWarning("Request error: {Message} at {Path}. TraceId: {TraceId}", ex.Message, context.Request.Path, traceId);
                }
                else
                {
                    // Если это 500-ка, значит это реальный баг. Логируем со всем стеком (LogError)
                    _logger.LogError(ex, "Server error occurred at {Path}. TraceId: {TraceId}", context.Request.Path, traceId);
                }

                context.Response.StatusCode = statusCode;

                var problem = new ProblemDetails
                {
                    Title = GetTitle(ex),
                    Status = context.Response.StatusCode,
                    Detail = statusCode >= 500
                        ? "An unexpected server error occurred. Please try again later."
                        : ex.Message,
                    Instance = context.Request.Path
                };

                await _problemDetailsService.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails = problem
                });
            }
        }

        private static int GetStatusCode(Exception ex) => ex switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            NoAvailableSeatsException => StatusCodes.Status409Conflict,
            OperationCanceledException => StatusCodes.Status499ClientClosedRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        private static string GetTitle(Exception ex) => ex switch
        {
            ValidationException => "Bad request",
            NotFoundException => "Not found",
            NoAvailableSeatsException => "No available seats for this event",
            OperationCanceledException => "The operation was canceled",
            _ => "Internal server error"
        };
    }
}
