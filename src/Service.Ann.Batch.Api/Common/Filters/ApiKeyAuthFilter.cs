using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Service.Ann.Batch.Api.Application.Wrappers;


namespace Service.Ann.Batch.Api.Common.Filters;

public class ApiKeyAuthFilter : IAuthorizationFilter
{
    private const string APIKEYNAME = "x-api-key";
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        string message = string.Empty;
        if (!IsAuthorized(context, ref message))
        {
            context.Result = new UnauthorizedObjectResult(new ErrorResponse { Message = message });
            return;
        }
    }

    private static bool IsAuthorized(AuthorizationFilterContext context, ref string message)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(APIKEYNAME, out var extractedApiKey))
        {
            message = "Api Key was not provided";
            return false;
        }
        var appSettings = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

        string apiKey = appSettings.GetValue<string>("ApiKey")!;
        if (!apiKey.Equals(extractedApiKey))
        {
            message = "Api Key is not valid";
            return false;
        }
        return true;
    }
}
