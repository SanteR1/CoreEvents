using Bookings.Application.Abstractions.Messaging;
using Bookings.Application.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, Action<ApplicationOptions> configureOptions)
    {
        var options = new ApplicationOptions();
        configureOptions(options);
        services.AddSingleton(options.Booking);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<ICommand>();
        });

        return services;
    }
}
