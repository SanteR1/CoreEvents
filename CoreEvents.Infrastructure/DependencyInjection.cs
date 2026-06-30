using CoreEvents.Application.Interfaces.Repositories;
using CoreEvents.Infrastructure.Data;
using CoreEvents.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;


namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            services.AddDataBase(configuration, environment);
            services.AddScoped<IBookingRepository, BookingRepository>();
            services.AddScoped<IEventRepository, EventRepository>();

            return services;
        }
        public static IServiceCollection AddDataBase(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                                   ?? throw new InvalidOperationException("Connection string 'Default' not found.");

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
                if (!environment.IsProduction())
                {
                    options
                        .LogTo(Console.WriteLine)
                        .EnableDetailedErrors();
                }
            });

            return services;
        }
    }
}
