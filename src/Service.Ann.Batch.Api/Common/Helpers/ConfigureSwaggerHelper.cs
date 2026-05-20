using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace Service.Ann.Batch.Api.Common.Helpers;

public class ConfigureSwaggerHelper : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Service.Ann.Batch.Api",
            Version = "v1",
            Description = "REST API for Batched Announcement."
        });

        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }

        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "x-api-key",
            Description = "Enter your API key to authorize requests.",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "x-api-key"
            }
        };

        options.AddSecurityDefinition("x-api-key", securityScheme);

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                securityScheme,
                new List<string>()
            }
        });
    }
}