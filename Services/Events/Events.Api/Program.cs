var builder = WebApplication.CreateBuilder(args);

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
await app.Services.InitializeKafkaTopicsAsync();

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

app.Run();
