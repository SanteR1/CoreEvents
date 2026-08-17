using Bookings.Application.Abstractions.Repositories;
using Bookings.Infrastructure.Data;
using Bookings.IntegrationTests.Infrastructure.FaultInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Bookings.IntegrationTests.Infrastructure.Factories;

public class FaultInjectionTestFactory : IntegrationTestFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.UseSetting("BackgroundServices:BookingInterval", "10");

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<FaultInjectionState>();

            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IBookingRepository));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddScoped<IBookingRepository>(sp =>
            {
                Type implementationType = descriptor!.ImplementationType
                                          ?? throw new InvalidOperationException("Original repository implementation type is unknown.");

                var innerRepository = (IBookingRepository)ActivatorUtilities.CreateInstance(sp, implementationType);

                var dbContext = sp.GetRequiredService<BookingsDbContext>();

                var state = sp.GetRequiredService<FaultInjectionState>();

                return new FaultInjectingBookingRepository(innerRepository, state, dbContext);
            });

            services.AddScoped(sp => (FaultInjectingBookingRepository)sp.GetRequiredService<IBookingRepository>());
        });
    }
}
