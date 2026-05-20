using Microsoft.AspNetCore.Mvc;
using Service.Ann.Batch.Api.Application.Wrappers;

namespace Service.Ann.Batch.Api.Common.Behaviors;

public class BehaviorBadRequest
{
    public static void ParseModelErrors(ApiBehaviorOptions options)
    {
        options.InvalidModelStateResponseFactory = (context) =>
        {
            ErrorResponse responseError = new() { Succeeded = false, Message = "One or more validations occurred.", Errors = null };
            if (!context.ModelState.IsValid)
            {
                responseError.Errors = [.. context.ModelState.Values
                        .Where(v => v.Errors.Count > 0)
                        .SelectMany(v => v.Errors)
                        .Select(v => v.ErrorMessage)
                        .Distinct()];
            }
            return new BadRequestObjectResult(responseError);
        };
    }
}
