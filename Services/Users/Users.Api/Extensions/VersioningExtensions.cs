using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;

namespace Users.Api.Extensions;

public static class VersioningExtensions
{
    public static IServiceCollection AddPresentationVersioningAndOpenApi(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
                        {
                            options.ReportApiVersions = true;
                            options.ApiVersionReader = new UrlSegmentApiVersionReader();
                        })
                        .AddMvc()
                        .AddApiExplorer(options =>
                        {
                            options.GroupNameFormat = "'v'VVV";
                            options.SubstituteApiVersionInUrl = true;
                        }).AddOpenApi(options =>
                        {
                            options.Document.AddDocumentTransformer((document, context, cancellationToken) =>
                            {
                                document.Servers = [new OpenApiServer { Url = "/" }];
                                document.Components ??= new OpenApiComponents();
                                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.Ordinal);
                                document.Components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
                                {
                                    Type = SecuritySchemeType.Http,
                                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                                    BearerFormat = "JWT",
                                    Description = "Введите JWT токен"
                                };

                                document.Security ??= new List<OpenApiSecurityRequirement>();
                                document.Security.Add(new OpenApiSecurityRequirement
                                {
                                    [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, document)] = []
                                });
                                return Task.CompletedTask;
                            });
                        });
        return services;
    }
}
