using System.Text;
using Bookings.Application.Abstractions;
using Bookings.Application.Abstractions.Messaging;
using Bookings.Application.Abstractions.Resilience.Constants;
using Bookings.Infrastructure.Data;
using Bookings.Infrastructure.Data.Entities;
using Bookings.Infrastructure.Messaging.Kafka;
using Bookings.Infrastructure.Messaging.Options;
using Confluent.Kafka;
using CoreEvents.Shared.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Registry;

namespace Bookings.Infrastructure.Messaging;

sealed class KafkaConsumerBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> kafkaOptions,
    ResiliencePipelineProvider<string> pipelineProvider,
    ICorrelationContext correlationContext,
    ILogger<KafkaConsumerBackgroundService> logger)
    : BackgroundService
{

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Factory.StartNew(
            () => Consume(stoppingToken),
            stoppingToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default).Unwrap();
    }

    private async Task Consume(CancellationToken stoppingToken)
    {
        var options = kafkaOptions.Value;
        var config = new ConsumerConfig
        {
            BootstrapServers = options.BootstrapServers,
            GroupId = options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false
        };
        using var consumer = new ConsumerBuilder<string, string>(config).Build();

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = options.BootstrapServers,
            Acks = Acks.All // Гарантируем, что брокер точно сохранил сообщение в DLT
        };
        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        consumer.Subscribe(KafkaTopics.EventConfirmed);
        logger.LogInformation("Kafka Consumer started for topic: {Topic}", KafkaTopics.EventConfirmed);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result = null;

                try
                {
                    result = consumer.Consume(stoppingToken);
                    if (result == null || result.Message == null) continue;

                    var jsonPayload = result.Message.Value;

                    // 1. Предварительная проверка (Tombstone)
                    if (string.IsNullOrEmpty(jsonPayload))
                    {
                        logger.LogWarning("Пустое сообщение (tombstone?) на offset {Offset}, пропускаем.",
                            result.Offset.Value);
                        consumer.Commit(result);
                        continue;
                    }


                    // 2. Метаданные — из headers, ДО парсинга payload
                    var metadata = EventMetadataHeaderMapper.ParseMetadata(result.Message.Headers);

                    if (metadata.CorrelationId == Guid.Empty)
                    {
                        logger.LogError(
                            "Сообщение {MessageId} из топика {Topic} пришло без CorrelationId в headers",
                            metadata.MessageId, result.Topic);
                    }

                    correlationContext.SetCorrelationId(metadata.CorrelationId);
                    correlationContext.SetCausationId(metadata.MessageId);

                    // 3. Основной пайплайн (Infrastructure Resilience)
                    var infraPipeline = pipelineProvider.GetPipeline("global-transient-pipeline");

                    await infraPipeline.ExecuteAsync(async token =>
                    {
                        using var scope = scopeFactory.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
                        var dispatcher = scope.ServiceProvider.GetRequiredService<IIntegrationEventDispatcher>();



                        await using var transaction = await dbContext.Database.BeginTransactionAsync(token);
                        try
                        {
                            // А) Idempotency Check (Inbox)
                            bool isProcessed = await dbContext.InboxMessages
                                .AnyAsync(m => m.Id == metadata.MessageId, token);

                            if (!isProcessed)
                            {
                                // Б) Business Logic (внутри Mediator, который имеет свой Concurrency Retry)
                                //await mediator.Send(
                                //    new CreateBookingCommand(
                                //        incomingMessage.EventId,
                                //        incomingMessage.UserId,
                                //        incomingMessage.Seats),
                                //    token);

                                await dispatcher.DispatchAsync(metadata.EventType, jsonPayload, token);


                                // В) Inbox Registration
                                // public string? MessageType { get; set; }
                                // public string? HandlerName { get; set; }
                                dbContext.InboxMessages.Add(
                                    new InboxMessage()
                                    {
                                        Id = metadata.MessageId,
                                        CorrelationId = metadata.CorrelationId,
                                        CausationId = metadata.CausationId,

                                        ConsumerName = options.GroupId,
                                        Topic = result.Topic,

                                        Partition = result.Partition.Value,
                                        Offset = result.Offset.Value,

                                        MessageKey = result.Message.Key ?? string.Empty,
                                        MessageType = metadata.EventType,

                                        Payload = result.Message.Value,
                                        Headers = EventMetadataHeaderMapper.SerializeHeaders(result.Message.Headers),

                                        ReceivedAt = DateTimeOffset.UtcNow,
                                        ProcessedAt = DateTimeOffset.UtcNow,
                                        LastError = null
                                    });
                            }

                            await dbContext.SaveChangesAsync(token);
                            await transaction.CommitAsync(token);
                        }
                        catch (Exception)
                        {
                            await transaction.RollbackAsync(token);
                            throw; // Важно! Пробрасываем ошибку, чтобы Polly ее поймал и ретраил
                        }
                    }, stoppingToken);

                    // 4. Успех — фиксируем оффсет
                    consumer.Commit(result);
                }
                catch (OperationCanceledException ex)
                {
                    logger.LogInformation("Kafka остановлен. {message}", ex.Message);
                    break;
                }
                catch (Exception ex)
                {
                    // 5. Пытаемся спасти сообщение через DLT
                    if (result != null)
                    {
                        try
                        {
                            var dltPipeline = pipelineProvider.GetPipeline(ResiliencePipelines.GlobalTransient);
                            using var dltCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                            await dltPipeline.ExecuteAsync(async token =>
                                await MoveToDeadLetterTopicAsync(producer, result, ex, token), dltCts.Token);

                            consumer.Commit(result);
                        }
                        catch (Exception dltEx)
                        {
                            // Если даже DLT не сработал — Останавливаем весь сервис!
                            logger.LogCritical(dltEx, "DLT Unavailable. Crashing to prevent data loss.");
                            throw;
                        }
                    }
                    else
                    {
                        logger.LogError(ex, "Unexpected error in consumer loop.");
                        throw;
                    }
                }
            }
        }
        finally
        {
            logger.LogInformation("Closing Kafka Consumer and Producer...");
            producer.Flush(stoppingToken);
            consumer.Close();
        }
    }

    private async Task MoveToDeadLetterTopicAsync(IProducer<string, string> producer, ConsumeResult<string, string> result, Exception exception,
        CancellationToken ct)
    {
        // Логика отправки в DLT
        logger.LogError(exception, "Message {Key} failed all retries. Moving to DLT.", result.Message.Key);
        var dltTopicName = KafkaTopics.EventConfirmedDlt;
        var dltReason = $"Error: {exception.Message}";
        var dltMessage = new Message<string, string>
        {
            Key = result.Message.Key, // Сохраняем оригинальный ключ партиционирования
            Value = result.Message.Value, // Оригинальный JSON
            Headers = result.Message.Headers // Оригинальные Headers
        };
        dltMessage.Headers.Add("error-Reason", Encoding.UTF8.GetBytes(dltReason));
        dltMessage.Headers.Add("error-ExceptionType", Encoding.UTF8.GetBytes(exception.GetType().FullName ?? exception.GetType().Name));
        dltMessage.Headers.Add("error-SourceTopic", Encoding.UTF8.GetBytes(result.Topic));
        dltMessage.Headers.Add("error-SourcePartition", Encoding.UTF8.GetBytes(result.Partition.Value.ToString()));
        dltMessage.Headers.Add("error-SourceOffset", Encoding.UTF8.GetBytes(result.Offset.Value.ToString()));
        dltMessage.Headers.Add("error-Timestamp", Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")));

        // Пытаемся записать в DLT
        await producer.ProduceAsync(dltTopicName, dltMessage, ct);
    }

}
