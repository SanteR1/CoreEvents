using System.Text.Json;
using Bookings.Application.Abstractions.Messaging;
using Bookings.Application.Abstractions.Persistence;
using Bookings.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bookings.Infrastructure.Messaging;

internal sealed class OutboxBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceProvider;
    private readonly ILogger<OutboxBackgroundWorker> _logger;
    private readonly IMessageProducer _messageProducer;
    private readonly IExceptionAnalyzer _exceptionAnalyzer;
    private const int _maxRetryCount = 5;

    public OutboxBackgroundWorker(
        IServiceScopeFactory serviceProvider,
        ILogger<OutboxBackgroundWorker> logger,
        IMessageProducer messageProducer,
        IExceptionAnalyzer exceptionAnalyzer)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _messageProducer = messageProducer;
        _exceptionAnalyzer = exceptionAnalyzer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxBackgroundWorker запущен.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка в цикле обработки Outbox.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }

        _logger.LogInformation("OutboxBackgroundWorker остановлен.");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.PublishedAt == null && !m.IsDeadLettered && (m.NextRetryAt == null || m.NextRetryAt <= DateTimeOffset.UtcNow))
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        if (messages.Count == 0) return;

        foreach (var message in messages)
        {
            try
            {
                var headers = string.IsNullOrWhiteSpace(message.Headers)
                    ? null
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(message.Headers);

                await _messageProducer.ProduceAsync(
                    topic: message.Topic,
                    key: message.Key, // В будущем можем быть пустым для round-robin
                    payload: message.Payload,
                    headers: headers,
                    cancellationToken: ct);

                message.PublishedAt = DateTimeOffset.UtcNow;
            }
            catch (Exception ex) when (_exceptionAnalyzer.IsTransient(ex))
            {
                // Временная ошибка (сеть, брокер недоступен, таймаут)
                _logger.LogWarning(ex, "Ошибка Kafka при отправке сообщения {Id}", message.Id);
                message.NextRetryAt = DateTimeOffset.UtcNow.AddSeconds(5);
                break;// весь батч на любой transient-ошибке
            }
            catch (Exception ex)
            {
                // Ошибка десериализации заголовков или бизнес-логики
                _logger.LogError(ex, "Ошибка обработки Outbox сообщения {Id}", message.Id);
                message.RetryCount++;
                message.LastError = $"Внутренняя ошибка: {ex.Message}";
                if (message.RetryCount >= _maxRetryCount)
                {
                    // TODO здесь можно перевести в некий "dead" статус — отдельное поле IsDead / MovedToDlq
                    _logger.LogCritical(ex, "Outbox сообщение {Id} превысило лимит попыток, требует ручного вмешательства", message.Id);
                    message.IsDeadLettered = true;
                }
                else
                {
                    var delaySeconds = Math.Pow(2, message.RetryCount);
                    message.NextRetryAt = DateTimeOffset.UtcNow.AddSeconds(delaySeconds);
                }
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
