using CoreEvents.Application.Configuration;
using CoreEvents.Application.Orchestrators;
using CoreEvents.Application.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, Action<ApplicationOptions> configureOptions)
    {
        var options = new ApplicationOptions();
        configureOptions(options);
        services.AddSingleton(options.Booking);

        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IBookingOrchestrator, BookingOrchestrator>();

        return services;
    }
}
