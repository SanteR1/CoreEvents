namespace Events.Application.Abstractions.Messaging;

public interface IMessageProducer
{
    Task ProduceAsync(
        string topic,
        string key,
        string payload,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);
}
