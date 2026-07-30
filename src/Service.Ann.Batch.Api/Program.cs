using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using Service.Ann.Batch.Api.Common.Extensions;
using Service.Ann.Batch.Api.Common.Middleware;
using Service.Ann.Batch.Api.Infrastructure.Security;


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
            .WithOrigins("http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader();
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

builder.Services.AddAuthorization();


var app = builder.Build();

app.UseRouting();

app.UseCors("AllowReact");

app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.Headers.Add("Access-Control-Allow-Origin", "http://localhost:3000");
        context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, X-Api-Key");
        context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        context.Response.StatusCode = 200;
        return;
    }

    await next();
});


// Configure the HTTP request pipeline.

var scalarDocEnabled = builder.Configuration.GetValue<bool>("ScalarDocEnabled");
if (app.Environment.IsDevelopment() || scalarDocEnabled)

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

app.Run();
