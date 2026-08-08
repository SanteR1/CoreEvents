using Events.Application.Abstractions.Messaging;
using Events.Application.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<ICommand>();
        });

        services.AddScoped<IEventService, EventService>();
        return services;
    }


}
