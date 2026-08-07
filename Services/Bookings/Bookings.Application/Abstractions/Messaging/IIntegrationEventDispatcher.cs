namespace Bookings.Application.Abstractions.Messaging
{
    public interface IIntegrationEventDispatcher
    {
        Task DispatchAsync(string eventType, string payload, CancellationToken ct);
    }
}
