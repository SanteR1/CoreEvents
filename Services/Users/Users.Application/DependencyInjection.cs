using Users.Application.Interfaces.Services;
using Users.Application.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {

        services.AddScoped<IAuthService, UserService>();

        return services;
    }
}
