using Serilog;
using Service.Ann.Batch.Api.Common.Extensions;
using Service.Ann.Batch.Api.Common.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Serilog ConfigurationA
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddDependency(builder.Configuration);
builder.Services.AddHealthChecksExtension();
builder.Services.AddFluentValidationExtension();
builder.Services.AddCors();

var app = builder.Build();

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

app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapControllers();

app.Run();
