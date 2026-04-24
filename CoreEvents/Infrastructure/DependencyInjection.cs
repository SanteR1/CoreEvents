using CoreEvents.Data.Queues;
using CoreEvents.Data.Repositories;
using CoreEvents.Infrastructure.BackgroundServices;
using CoreEvents.Models.Domain;
using CoreEvents.Services.Implementations;
using CoreEvents.Services.Interfaces;

namespace CoreEvents.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IRepository<EventEntity>, InMemoryRepository<EventEntity>>();
            services.AddSingleton<IQueueSource<Guid>, InMemoryBookingQueue>();
            services.AddSingleton<IRepository<Booking>, InMemoryBookingRepository<Booking>>();
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddHostedService<BookingProcessingService>();

            return services;
        }
    }
}
