using CoreEvents.Data.DataAccess;
using CoreEvents.Infrastructure.BackgroundServices;
using CoreEvents.Services.Implementations;
using CoreEvents.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CoreEvents.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                                   ?? throw new InvalidOperationException("Connection string 'Default' not found.");

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString)
                    .LogTo(Console.WriteLine)
                    .EnableDetailedErrors());

            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddHostedService<BookingProcessingService>();

            return services;
        }
    }
}
