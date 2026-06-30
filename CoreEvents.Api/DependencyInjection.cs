using System.Text.Json.Serialization;
using CoreEvents.Api.BackgroundServices;

namespace CoreEvents.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        services.AddHostedService<BookingProcessingService>();
        services.AddOpenApi();
        services.AddSwaggerGen();
        services.AddProblemDetails();

        return services;
    }
}