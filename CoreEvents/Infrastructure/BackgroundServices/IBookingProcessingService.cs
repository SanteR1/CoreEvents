using CoreEvents.Models.Domain;

namespace CoreEvents.Infrastructure.BackgroundServices
{
    public interface IBookingProcessingService
    {
        public Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken);
    }
}
