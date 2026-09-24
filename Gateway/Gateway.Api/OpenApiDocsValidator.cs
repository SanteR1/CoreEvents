using Microsoft.Extensions.Options;
namespace Gateway.Api;
/// <summary>
/// Фоновый сервис для разовой проверки доступности сконфигурированных спецификаций OpenAPI при старте.
/// Использует ретраи с паузой для безопасного ожидания завершения миграций в зависимых сервисах.
/// </summary>
public sealed class OpenApiDocsValidator : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<List<OpenApiDocConfig>> _docs;
    private readonly ILogger<OpenApiDocsValidator> _logger;

    public OpenApiDocsValidator(IHttpClientFactory httpClientFactory, IOptions<List<OpenApiDocConfig>> docs, ILogger<OpenApiDocsValidator> logger)
    {
        _httpClientFactory = httpClientFactory;
        _docs = docs;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Небольшая задержка перед первой попыткой для холодного старта зависимостей
        await Task.Delay(TimeSpan.FromSeconds(3), ct);

        using var client = _httpClientFactory.CreateClient("openapi-validator");
        foreach (var doc in _docs.Value)
        {
            bool isSuccess = false;

            for (int attempt = 1; attempt <= 5; attempt++)
            {
                try
                {
                    using var response = await client.GetAsync(doc.Url, ct);
                    if (response.IsSuccessStatusCode)
                    {
                        isSuccess = true;
                        _logger.LogInformation("OpenApiDocs: Спецификация [{Key}] успешно проверена по адресу {Url}", doc.Key, doc.Url);
                    }
                    else if (attempt < 5)
                    {
                        _logger.LogDebug("Попытка {Attempt} для {Key} вернула статус {Status}, повтор через 2с",
                        attempt, doc.Key, (int)response.StatusCode);
                        await Task.Delay(TimeSpan.FromSeconds(2), ct);
                    }
                }
                catch (Exception ex) when (attempt < 5 && !ct.IsCancellationRequested)
                {
                    _logger.LogDebug(ex, "Попытка {Attempt} для {Key} не удалась, повтор через 2с", attempt, doc.Key);
                    await Task.Delay(TimeSpan.FromSeconds(2), ct);
                }
            }

            if (!isSuccess && !ct.IsCancellationRequested)
            {
                _logger.LogWarning("OpenApiDocs: Спецификация [{Key}] недоступна по адресу {Url} после 5 попыток", doc.Key, doc.Url);
            }
        }

    }
}
