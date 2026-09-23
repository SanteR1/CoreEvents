using Gateway.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Регистрация сервисов шлюза
builder.Services.AddGatewayCors(builder.Configuration);
builder.Services.AddGatewayRateLimiting(builder.Configuration);
builder.Services.AddGatewayDocumentation(builder.Configuration, builder.Environment);
builder.Services.AddGatewayReverseProxy(builder.Configuration);

var app = builder.Build();

// Настройка конвейера обработки запросов
app.UseCors("CorsPolicy");
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseGatewayDocumentation();
}

app.MapReverseProxy();

app.Run();

public partial class Program;
