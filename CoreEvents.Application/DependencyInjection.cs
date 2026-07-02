using CoreEvents.Application.Orchestrators;
using CoreEvents.Application.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IBookingOrchestrator, BookingOrchestrator>();

        return services;
    }
}
