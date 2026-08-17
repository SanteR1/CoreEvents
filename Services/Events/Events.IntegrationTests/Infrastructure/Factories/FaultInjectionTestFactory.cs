using Events.Application.Abstractions.Repositories;
using Events.Infrastructure.Data;
using Events.IntegrationTests.Infrastructure.FaultInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Events.IntegrationTests.Infrastructure.Factories;

public class FaultInjectionTestFactory : IntegrationTestFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.UseSetting("BackgroundServices:BookingInterval", "10");

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<FaultInjectionState>();

            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEventRepository));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddScoped<IEventRepository>(sp =>
            {
                Type implementationType = descriptor!.ImplementationType
                                          ?? throw new InvalidOperationException("Original repository implementation type is unknown.");

                var innerRepository = (IEventRepository)ActivatorUtilities.CreateInstance(sp, implementationType);

                var dbContext = sp.GetRequiredService<EventsDbContext>();

                var state = sp.GetRequiredService<FaultInjectionState>();

                return new FaultInjectingEventRepository(innerRepository, state, dbContext);
            });

            services.AddScoped(sp => (FaultInjectingEventRepository)sp.GetRequiredService<IEventRepository>());
        });
    }
}
