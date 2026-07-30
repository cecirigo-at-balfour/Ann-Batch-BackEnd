using Microsoft.AspNetCore.Builder;
using SwaggerHierarchySupport;
using Scalar.AspNetCore;

namespace Service.Ann.Batch.Api.Common.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseApiDocumentationExtension(this WebApplication app)
    {
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/v1/swagger.json", "Service.As400.Data.Api v1");
            options.AddHierarchySupport();
        });

        app.UseSwagger(option =>
        {
            option.RouteTemplate = "openapi/{documentName}.json";
        });

        app.MapScalarApiReference(options => {
            options.WithTitle("Service.Ann.Batch.Api")
            .WithTheme(ScalarTheme.Mars)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.Http);
            options.Layout = ScalarLayout.Modern;
            options.DarkMode = true;
        });

        return app;
    }
}