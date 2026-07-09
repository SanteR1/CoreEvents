using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using CoreEvents.Presentation.BackgroundServices;

namespace CoreEvents.IntegrationTests.Infrastructure.Factories;


/// <summary>
/// Запуск без фонового обработчика бронирования
/// </summary>
public class ApiOnlyIntegrationTestFactory : IntegrationTestFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IHostedService) &&
                d.ImplementationType == typeof(BookingProcessingService));

            if (descriptor != null)
                services.Remove(descriptor);
        });
    }
}
