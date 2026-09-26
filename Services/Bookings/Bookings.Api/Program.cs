using Bookings.Api.Extensions;
using Bookings.Api.Middlewares;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
             .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
             .Enrich.FromLogContext()
             .WriteTo.Console(formatter: new CompactJsonFormatter())
             .CreateBootstrapLogger();

try
{
    Log.Information("Starting Bookings.Api service");

    var builder = WebApplication.CreateBuilder(args);


    builder.AddApplicationLogging();
    builder.Services.AddApplicationTelemetry(builder.Configuration);

    if (builder.Environment.IsDevelopment())
    {
        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });
    }

    builder.Services.AddPresentationServices();
    builder.Services.AddApplicationServices(options =>
    {
        builder.Configuration.GetSection("ApplicationSettings").Bind(options);
    });
    builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);

    var app = builder.Build();

    app.UseExceptionHandler();

    app.UseMiddleware<CorrelationIdMiddleware>();

    await app.ApplyMigrationsAsync();
    await app.Services.InitializeKafkaTopicsAsync();


    app.UseForwardedHeaders();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi().WithDocumentPerVersion();

        app.UseSwaggerUI(options =>
        {
            foreach (var groupName in app.DescribeApiVersions().Select(d => d.GroupName))
            {
                options.SwaggerEndpoint($"/openapi/{groupName}.json", groupName.ToUpperInvariant());
            }
            options.EnablePersistAuthorization();
        });

        app.MapScalarApiReference(options =>
        {
            var descriptions = app.DescribeApiVersions();
            for (var i = 0; i < descriptions.Count; i++)
            {
                var description = descriptions[i];
                var isDefault = i == descriptions.Count - 1;
                options.AddDocument(description.GroupName, description.GroupName, isDefault: isDefault);
            }

            // 1. Сохранять введенный токен в LocalStorage браузера при перезагрузке страницы:
            options.EnablePersistentAuthentication();

            // 2. Сделать Bearer схемой по умолчанию при открытии страницы:
            options.AddPreferredSecuritySchemes("Bearer");
        });
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapPrometheusScrapingEndpoint();

    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application Bookings.Api service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
