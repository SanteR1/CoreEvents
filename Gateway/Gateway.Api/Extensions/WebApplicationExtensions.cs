using Scalar.AspNetCore;

namespace Gateway.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseGatewayDocumentation(this WebApplication app)
    {
        var openApiDocs = app.Configuration.GetSection("OpenApiDocs").Get<List<OpenApiDocConfig>>() ?? [];

        // Swagger UI
        app.UseSwaggerUI(options =>
        {
            foreach (var doc in openApiDocs)
            {
                options.SwaggerEndpoint(doc.Url, doc.Title);
            }
            options.RoutePrefix = "swagger";
            options.EnablePersistAuthorization();
        });

        // Scalar UI
        app.MapScalarApiReference("/docs", options =>
        {
            for (var i = 0; i < openApiDocs.Count; i++)
            {
                var doc = openApiDocs[i];
                var relativeUrl = doc.Url.StartsWith('/') ? $"..{doc.Url}" : doc.Url;
                options.AddDocument(doc.Key, doc.Title, relativeUrl, isDefault: i == 0);
            }
            options.WithBaseServerUrl("/");
            options.EnablePersistentAuthentication();
            options.AddPreferredSecuritySchemes("Bearer");
        });

        return app;
    }
}