using System.Text;
using Confluent.Kafka;
using Events.Application.Abstractions.Messaging;
using Events.Infrastructure.Messaging.Options;
using Microsoft.Extensions.Options;

namespace Events.Infrastructure.Messaging.Kafka;

internal class MessageProducer : IMessageProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;

    public MessageProducer(IOptions<KafkaOptions> options)
    {
        var settings = options.Value;
        var config = new ProducerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            Acks = Acks.All
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task ProduceAsync(string topic, string key, string value, IDictionary<string, string>? headers = null, CancellationToken ct = default)
    {
        Headers? kafkaHeaders = null;
        if (headers != null && headers.Any())
        {
            kafkaHeaders = new Headers();
            foreach (var header in headers)
            {
                kafkaHeaders.Add(header.Key, Encoding.UTF8.GetBytes(header.Value));
            }
        }

        var message = new Message<string, string>
        {
            Key = key,
            Value = value,
            Headers = kafkaHeaders
        };

        await _producer.ProduceAsync(topic, message, ct);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}
