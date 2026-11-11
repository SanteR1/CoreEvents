using System.Globalization;
using System.Text;
using Confluent.Kafka;
using CoreEvents.Shared.Contracts.Infrastructure;
using Events.Application.Abstractions;
using Events.Application.Abstractions.Messaging;
using Events.Application.Abstractions.Resilience.Constants;
using Events.Infrastructure.Messaging.Kafka;
using Events.Infrastructure.Messaging.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Registry;

namespace Events.Infrastructure.Messaging;

sealed class KafkaConsumerBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> kafkaOptions,
    ResiliencePipelineProvider<string> pipelineProvider,
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
        using var consumer = CreateConsumer(options);
        using var producer = CreateProducer(options);

        consumer.Subscribe(KafkaTopics.BookingConfirmed);
        logger.LogInformation("Kafka Consumer started for topic: {Topic}", KafkaTopics.BookingConfirmed);

        try
        {
            await ConsumeLoopAsync(consumer, producer, options, stoppingToken);
        }
        finally
        {
            logger.LogInformation("Closing Kafka Consumer and Producer...");
            producer.Flush(stoppingToken);
            consumer.Close();
        }
    }

    private async Task ConsumeLoopAsync(
        IConsumer<string, string> consumer,
        IProducer<string, string> producer,
        KafkaOptions options,
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result = null;

            try
            {
                result = consumer.Consume(stoppingToken);
                if (result == null || result.Message == null) continue;

                // 1. Предварительная проверка (Tombstone)
                if (string.IsNullOrEmpty(result.Message.Value))
                {
                    logger.LogWarning(
                        "Пустое сообщение (tombstone?) на offset {Offset}, пропускаем.",
                        result.Offset.Value);
                    consumer.Commit(result);
                    continue;
                }

                await ProcessMessageAsync(consumer, result, options, stoppingToken);
            }
            catch (OperationCanceledException ex)
            {
                logger.LogInformation(ex, "Kafka остановлен: {Message}", ex.Message);
                break;
            }
            catch (Exception ex)
            {
                if (result == null)
                {
                    logger.LogError(ex, "Unexpected error in consumer loop.");
                    throw;
                }

                // Пытаемся спасти сообщение через DLT
                await HandleFailureWithDltAsync(producer, consumer, result, ex);
            }
        }
    }

    private async Task ProcessMessageAsync(
        IConsumer<string, string> consumer,
        ConsumeResult<string, string> result,
        KafkaOptions options,
        CancellationToken stoppingToken)
    {
        var metadata = EventMetadataHeaderMapper.ParseMetadata(result.Message.Headers);
        var envelope = CreateInboxEnvelope(result, metadata, options.GroupId);

        // 1. Создание нового изолированного scope на каждое сообщение
        using var scope = scopeFactory.CreateScope();

        // 2. Инициализация scoped-контекста трассировки для сообщений и Outbox
        var correlationContext = scope.ServiceProvider.GetRequiredService<ICorrelationContext>();
        correlationContext.SetCorrelationId(envelope.CorrelationId);
        correlationContext.SetCausationId(envelope.MessageId); // Входящее сообщение является причиной для исходящих действий

        // 3. Разрешение scoped-диспетчера (захватывает scoped IMediator и scoped DbContext/UnitOfWork)
        var dispatcher = scope.ServiceProvider.GetRequiredService<IIntegrationEventDispatcher>();

        // 4. Диспетчеризация через MediatR Pipeline (Polly -> TransactionBehavior -> InboxBehavior -> Handler)
        await dispatcher.DispatchAsync(metadata.EventType, result.Message.Value, envelope, stoppingToken);

        // 5. Оффсет фиксируется строго после успешного завершения пайплайна
        consumer.Commit(result);
    }

    private async Task HandleFailureWithDltAsync(
        IProducer<string, string> producer,
        IConsumer<string, string> consumer,
        ConsumeResult<string, string> result,
        Exception ex)
    {
        try
        {
            var dltPipeline = pipelineProvider.GetPipeline(ResiliencePipelines.GlobalTransient);
            using var dltCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            await dltPipeline.ExecuteAsync(
                async token => await MoveToDeadLetterTopicAsync(producer, result, ex, token),
                dltCts.Token);

            consumer.Commit(result);
        }
        catch (Exception dltEx)
        {
            logger.LogCritical(dltEx, "DLT Unavailable. Crashing to prevent data loss.");
            throw new InvalidOperationException("DLT Unavailable. Crashing to prevent data loss.", dltEx);
        }
    }

    private async Task MoveToDeadLetterTopicAsync(
        IProducer<string, string> producer,
        ConsumeResult<string, string> result,
        Exception exception,
        CancellationToken ct)
    {
        var category = exception switch
        {
            CoreEvents.Shared.Contracts.Exceptions.UnknownEventTypeException => "unknown_event",
            CoreEvents.Shared.Contracts.Exceptions.DataIntegrityException => "integrity",
            System.Text.Json.JsonException => "bad_payload",
            _ => "processing_error"
        };
        logger.LogError(exception, "[DLT:{Category}] Message {Key} failed. Moving to DLT.", category, result.Message.Key);

        var dltMessage = CreateDeadLetterMessage(result, exception);
        await producer.ProduceAsync(KafkaTopics.BookingConfirmedDlt, dltMessage, ct);
    }

    private static IConsumer<string, string> CreateConsumer(KafkaOptions options)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = options.BootstrapServers,
            GroupId = options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false
        };

        return new ConsumerBuilder<string, string>(config).Build();
    }

    private static IProducer<string, string> CreateProducer(KafkaOptions options)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = options.BootstrapServers,
            Acks = Acks.All
        };

        return new ProducerBuilder<string, string>(producerConfig).Build();
    }

    private static InboxEnvelope CreateInboxEnvelope(
        ConsumeResult<string, string> result,
        EventMetadata metadata,
        string consumerGroup) => new(
            MessageId: metadata.MessageId,
            CorrelationId: metadata.CorrelationId,
            CausationId: metadata.CausationId,
            ConsumerName: consumerGroup,
            Topic: result.Topic,
            Partition: result.Partition.Value,
            Offset: result.Offset.Value,
            MessageKey: result.Message.Key ?? string.Empty,
            MessageType: metadata.EventType,
            Payload: result.Message.Value,
            Headers: EventMetadataHeaderMapper.SerializeHeaders(result.Message.Headers),
            ReceivedAt: DateTimeOffset.UtcNow);

    private static Message<string, string> CreateDeadLetterMessage(
        ConsumeResult<string, string> result,
        Exception exception)
    {
        var dltReason = $"Error: {exception.Message}";
        var dltMessage = new Message<string, string>
        {
            Key = result.Message.Key,
            Value = result.Message.Value,
            Headers = result.Message.Headers
        };

        dltMessage.Headers.Add("error-Reason", Encoding.UTF8.GetBytes(dltReason));
        dltMessage.Headers.Add("error-ExceptionType", Encoding.UTF8.GetBytes(exception.GetType().FullName ?? exception.GetType().Name));
        dltMessage.Headers.Add("error-SourceTopic", Encoding.UTF8.GetBytes(result.Topic));
        dltMessage.Headers.Add("error-SourcePartition", Encoding.UTF8.GetBytes(result.Partition.Value.ToString(CultureInfo.InvariantCulture)));
        dltMessage.Headers.Add("error-SourceOffset", Encoding.UTF8.GetBytes(result.Offset.Value.ToString(CultureInfo.InvariantCulture)));
        dltMessage.Headers.Add("error-Timestamp", Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")));

        return dltMessage;
    }
}
