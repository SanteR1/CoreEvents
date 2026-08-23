using Serilog;

namespace Events.Api.Extensions;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddApplicationLogging(this WebApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, lc) => lc
                                                      .ReadFrom.Configuration(builder.Configuration)
                                                      .ReadFrom.Services(services)
                                                      .Enrich.FromLogContext());
        return builder;
    }
}
