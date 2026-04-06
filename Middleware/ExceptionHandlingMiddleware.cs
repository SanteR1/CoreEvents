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

                // Если это ошибка клиента (400, 404), логируем коротко как Warning
                if (statusCode < 500)
                {
                    _logger.LogWarning("Request error: {Message} at {Path}", ex.Message, context.Request.Path);
                }
                else
                {
                    // Если это 500-ка, значит это реальный баг. Логируем со всем стеком (LogError)
                    _logger.LogError(ex, "Server error occurred at {Path}", context.Request.Path);
                }

                context.Response.StatusCode = statusCode;

                var problem = new ProblemDetails
                {
                    Title = GetTitle(ex),
                    Status = context.Response.StatusCode,
                    Detail = ex.Message,
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
            ArgumentException => StatusCodes.Status400BadRequest,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        private static string GetTitle(Exception ex) => ex switch
        {
            ArgumentException => "Bad request",
            KeyNotFoundException => "Not found",
            _ => "Internal server error"
        };
    }
}
