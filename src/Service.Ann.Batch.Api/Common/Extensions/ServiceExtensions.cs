using FluentValidation;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using Service.Ann.Batch.Api.Common.Helpers;
using Service.Ann.Batch.Api.Common.Behaviors;
using Service.Ann.Batch.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Infrastructure.DataAccess.Baan;
using Service.Ann.Batch.Api.Common.Settings;
using Refit;

namespace Service.Ann.Batch.Api.Common.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddDependency(this IServiceCollection services, IConfiguration configuration)
    {
       
        var mySqlConn = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(mySqlConn))
            throw new InvalidOperationException("DefaultConnection (MySQL) is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(mySqlConn, ServerVersion.AutoDetect(mySqlConn)));

        services.AddRefitClient<IPrintboxApi>().ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri("https://balfour-pbx2.getprintbox.com");
    });

        services.AddScoped<IBaanRepository, BaanRepository>();
      
        var appConfig = new AppSettings();
        configuration.GetSection("AppSettings").Bind(appConfig);
        AppSettingsAnn.AppSettings = appConfig;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
        services.AddAutoMapper(typeof(Program).Assembly);
        services.AddValidatorsFromAssembly(typeof(Program).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services)
    { 
        services.AddControllers(options =>
        {
            //options.Filters.Add<GlobalExceptionHandlerMiddleware>();
        }).ConfigureApiBehaviorOptions(BehaviorBadRequest.ParseModelErrors)
        .AddNewtonsoftJson( options =>
        {
            options.SerializerSettings.Converters.Add(new StringEnumConverter
            {
                NamingStrategy = new DefaultNamingStrategy()
            });
        });

        services.AddSwaggerGen(options =>
        {
            options.EnableAnnotations();
        });
        services.ConfigureOptions<ConfigureSwaggerHelper>();
        services.AddEndpointsApiExplorer().AddSwaggerGenNewtonsoftSupport();
        services.AddAuthorization();
        services.AddCors();

        services.Configure<RouteOptions>(options =>
        {
            options.LowercaseUrls = true;
        });

        return services;      
    }

    public static void AddFluentValidationExtension(this IServiceCollection services)
    {
        services.AddFluentValidationRulesToSwagger();
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = false;
        });
    }

    public static IServiceCollection AddCorsExtension(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                builder =>
                {
                    builder.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                });
        });
        return services;
    }

    public static IServiceCollection AddHealthChecksExtension(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }
}
