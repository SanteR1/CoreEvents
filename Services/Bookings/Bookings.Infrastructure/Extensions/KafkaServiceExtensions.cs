using Bookings.Infrastructure.Messaging.Options;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using CoreEvents.Shared.Contracts.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bookings.Infrastructure.Extensions;

public static class KafkaServiceExtensions
{
    public static async Task InitializeKafkaTopicsAsync(
        this IServiceProvider services,
        CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(KafkaServiceExtensions));

        var kafkaOptions = scope.ServiceProvider
            .GetRequiredService<IOptions<KafkaOptions>>().Value;

        if (!kafkaOptions.InitKafkaTopics)
        {
            logger.LogInformation("Инициализация топиков Kafka отключена в конфигурации.");
            return;
        }

        await CreateTopicsCoreAsync(kafkaOptions, logger, ct);
    }

    private static async Task CreateTopicsCoreAsync(
        KafkaOptions kafkaOptions,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Начало проверки и создания топиков Kafka...");

        var config = new AdminClientConfig
        {
            BootstrapServers = kafkaOptions.BootstrapServers
        };

        using var adminClient = new AdminClientBuilder(config).Build();
        var topicsToCreate = BuildTopicSpecifications(kafkaOptions);

        try
        {
            ct.ThrowIfCancellationRequested();

            var options = new CreateTopicsOptions
            {
                RequestTimeout = TimeSpan.FromSeconds(kafkaOptions.TopicCreationTimeoutSeconds)
            };

            await adminClient.CreateTopicsAsync(topicsToCreate, options);
            logger.LogInformation("Все требуемые топики Kafka успешно созданы.");
        }
        catch (CreateTopicsException e)
        {
            ProcessCreateTopicsException(e, logger);
        }
        catch (KafkaException ex) when (ex.Error.Code == ErrorCode.Local_TimedOut)
        {
            logger.LogCritical(ex, "Не удалось связаться с Kafka по таймауту при создании топиков.");
            throw new InvalidOperationException("Не удалось связаться с Kafka по таймауту при создании топиков.", ex);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Инициализация топиков Kafka была отменена.");
            throw;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            logger.LogCritical(ex, "Критическая ошибка при инициализации топиков Kafka.");
            throw new InvalidOperationException("Критическая ошибка при инициализации топиков Kafka.", ex);
        }
    }

    private static List<TopicSpecification> BuildTopicSpecifications(KafkaOptions kafkaOptions) =>
    [
        new()
        {
            Name = KafkaTopics.EventConfirmed,
            NumPartitions = kafkaOptions.Topics.MainTopic.Partitions,
            ReplicationFactor = kafkaOptions.Topics.MainTopic.ReplicationFactor
        },
        new()
        {
            Name = KafkaTopics.EventConfirmedDlt,
            NumPartitions = kafkaOptions.Topics.DeadLetterTopic.Partitions,
            ReplicationFactor = kafkaOptions.Topics.DeadLetterTopic.ReplicationFactor
        }
    ];

    private static void ProcessCreateTopicsException(CreateTopicsException exception, ILogger logger)
    {
        var errors = new List<string>();

        foreach (var result in exception.Results)
        {
            if (result.Error.Code == ErrorCode.TopicAlreadyExists)
            {
                logger.LogInformation("Топик '{Topic}' уже существует. Пропускаем.", result.Topic);
                continue;
            }

            logger.LogError(exception, "Ошибка при создании топика '{Topic}': {Error}", result.Topic, result.Error.Reason);
            errors.Add($"Топик '{result.Topic}': {result.Error.Reason}");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Ошибки при создании топиков Kafka: {string.Join("; ", errors)}",
                exception);
        }
    }
}
