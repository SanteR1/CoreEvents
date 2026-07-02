using System.Text.Json.Serialization;
using CoreEvents.Presentation.BackgroundServices;
using CoreEvents.Presentation.ExceptionHandlers;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        services.AddProblemDetails();

        services.AddExceptionHandler<DomainExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddHostedService<BookingProcessingService>();
        services.AddOpenApi();
        services.AddSwaggerGen();

        return services;
    }
}