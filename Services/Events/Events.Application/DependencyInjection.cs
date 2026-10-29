using Events.Application.Abstractions.Messaging;
using Events.Application.Common.Dispatcher;
using Events.Application.Extensions;
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

        // Регистрация диспетчера доменных событий
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // Автоматическая регистрация всех IPreCommitHandler и IPostCommitHandler
        services.AddDomainEventHandlers(typeof(ICommand).Assembly);

        services.AddScoped<IEventService, EventService>();
        return services;
    }
}
