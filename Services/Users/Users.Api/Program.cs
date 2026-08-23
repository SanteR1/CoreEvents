using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Users.Api.Extensions;

Log.Logger = new LoggerConfiguration()
             .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
             .Enrich.FromLogContext()
             .WriteTo.Console(formatter: new CompactJsonFormatter())
             .CreateBootstrapLogger();

try
{
    Log.Information("Starting Users.Api service");

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
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);

    var app = builder.Build();

    app.UseExceptionHandler();

    await app.ApplyMigrationsAsync();
    await app.UseDatabaseSeedingAsync();

    app.UseAuthentication();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.EnablePersistAuthorization();
        });
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();
    app.MapPrometheusScrapingEndpoint();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application Users.Api service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
