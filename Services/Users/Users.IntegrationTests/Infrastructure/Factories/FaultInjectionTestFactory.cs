using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Users.Application.Interfaces.Repositories;
using Users.Infrastructure.Data;
using Users.IntegrationTests.Infrastructure.FaultInjection;

namespace Users.IntegrationTests.Infrastructure.Factories;

public class FaultInjectionTestFactory : IntegrationTestFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.UseSetting("BackgroundServices:BookingInterval", "10");

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<FaultInjectionState>();

            ServiceDescriptor? descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IUserRepository));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddScoped<IUserRepository>(sp =>
            {
                Type implementationType = descriptor!.ImplementationType
                                          ?? throw new InvalidOperationException(
                                              "Original repository implementation type is unknown.");

                IUserRepository innerRepository =
                    (IUserRepository)ActivatorUtilities.CreateInstance(sp, implementationType);

                UsersDbContext dbContext = sp.GetRequiredService<UsersDbContext>();

                FaultInjectionState state = sp.GetRequiredService<FaultInjectionState>();

                return new FaultInjectingUserRepository(innerRepository, state, dbContext);
            });

            services.AddScoped(sp => (FaultInjectingUserRepository)sp.GetRequiredService<IUserRepository>());
        });
    }
}
