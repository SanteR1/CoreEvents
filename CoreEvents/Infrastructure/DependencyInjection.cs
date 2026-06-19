using CoreEvents.Data.DataAccess;
using CoreEvents.Data.Repositories.Implementations;
using CoreEvents.Data.Repositories.Interfaces;
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
            services.AddDataBase(configuration);
            services.AddScoped<IBookingRepository, BookingRepository>();
            services.AddScoped<IEventRepository, EventRepository>();
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddHostedService<BookingProcessingService>();

            return services;
        }
        public static IServiceCollection AddDataBase(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                                   ?? throw new InvalidOperationException("Connection string 'Default' not found.");

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString)
                    .LogTo(Console.WriteLine)
                    .EnableDetailedErrors());

            return services;
        }
    }
}
