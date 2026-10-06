using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Service.Ann.Batch.Api.Common.Extensions;
using Service.Ann.Batch.Api.Common.Middleware;
using Service.Ann.Batch.Api.Infrastructure.Configuration;
using Service.Ann.Batch.Api.Infrastructure.Security;
using Service.Ann.Batch.Api.Infrastructure.Services;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

builder.Services.AddControllers();

// ✅ CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "http://ann-batch.corp-inet.com")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Serilog ConfigurationA
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddDependency(builder.Configuration);
builder.Services.AddHealthChecksExtension();
builder.Services.AddFluentValidationExtension();
builder.Services.AddCors();

var jwtSection = builder.Configuration.GetSection("Jwt");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtSection["Key"]!))
            };
    });

builder.Services.AddScoped<IFileResolverService, FileResolverService>();
builder.Services.Configure<ApiSettings>(
builder.Configuration.GetSection("ApiSettings"));

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseRouting();

app.UseCors("AllowReact");


// Configure the HTTP request pipeline.

var scalarDocEnabled = builder.Configuration.GetValue<bool>("ScalarDocEnabled");
if (scalarDocEnabled)

{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
    app.UseApiDocumentationExtension();
}

// Global exception middleware
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Request logging
app.UseSerilogRequestLogging();

// HTTPS
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapControllers();
Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
app.Run();
