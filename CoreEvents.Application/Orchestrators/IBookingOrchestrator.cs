namespace CoreEvents.Application.Orchestrators
{
    public interface IBookingOrchestrator
    {
        Task<IReadOnlyCollection<Guid>> GetWorkItemsAsync(CancellationToken cancellationToken);

        Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken);
    }
}
