using Bookings.Application.Abstractions;

namespace Bookings.Api.Middlewares
{
    public class CorrelationIdMiddleware(RequestDelegate next)
    {
        private const string CorrelationIdHeaderName = "X-Correlation-ID";

        public async Task InvokeAsync(HttpContext context, ICorrelationContext correlationContext)
        {
            Guid correlationId;

            // 1. Проверяем, прислал ли клиент свой ID в заголовках
            if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var headerValue) &&
                Guid.TryParse(headerValue, out var parsedId))
            {
                correlationId = parsedId;
            }
            else
            {
                // 2. Если нет — генерируем новый
                correlationId = Guid.NewGuid();
            }

            // 3. Устанавливаем в AsyncLocal контекст
            correlationContext.SetCorrelationId(correlationId);

            // 4. Опционально: добавляем ID в ответные заголовки, чтобы клиент мог его отследить
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[CorrelationIdHeaderName] = correlationId.ToString();
                return Task.CompletedTask;
            });

            await next(context);
        }
    }
}
