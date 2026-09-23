using System.Net.Http.Headers;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Gateway.Api.Transforms;

/// <summary>
/// Реализует паттерн BFF (Backend for Frontend).
/// Извлекает JWT access_token из HttpOnly Cookie браузера и преобразует его
/// в заголовок 'Authorization: Bearer <token>' перед отправкой запроса микросервису.
/// </summary>
public sealed class CookieToBearerTransformProvider : ITransformProvider
{
    public void Apply(TransformBuilderContext context)
    {
        context.AddRequestTransform(transformContext =>
    {
        // 1. Если клиент уже передал заголовок Authorization явно, не перезаписываем его:
        if (transformContext.HttpContext.Request.Headers.ContainsKey("Authorization"))
        {
            return ValueTask.CompletedTask;
        }

        // 2. Если в запросе присутсвтует HttpOnly Cookie "access_token", подставляем Bearer:
        if (transformContext.HttpContext.Request.Cookies.TryGetValue("access_token", out var accessToken)
        && !string.IsNullOrWhiteSpace(accessToken))
        {
            transformContext.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        return ValueTask.CompletedTask;
    });
    }

    public void ValidateRoute(TransformRouteValidationContext context) { }
    public void ValidateCluster(TransformClusterValidationContext context) { }
}