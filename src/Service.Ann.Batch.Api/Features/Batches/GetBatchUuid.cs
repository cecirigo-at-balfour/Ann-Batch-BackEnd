using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Application.Wrappers;
using Service.Ann.Batch.Api.Common.Settings;
using Service.Ann.Batch.Api.Infrastructure.Persistence;

namespace Service.Ann.Batch.Api.Features.Batch;

#region COMMAND

public record GetBatchUuidCommand(string Fo)
    : IRequest<SuccessResponse<string>>;

#endregion

#region VALIDATOR

public sealed class GetBatchUuidValidator
    : AbstractValidator<GetBatchUuidCommand>
{
    public GetBatchUuidValidator()
    {
        RuleFor(x => x.Fo)
            .NotEmpty();
    }
}

#endregion

#region CONTROLLER

[ApiController]
[Route("batch")]
[Tags("Batch")]
public class GetBatchUuidController(IMediator mediator)
    : ControllerBase
{
    [HttpPost("get-uuid")]
    public async Task<IActionResult> Post(
        [FromBody] GetBatchUuidCommand command,
        CancellationToken ct)
    {
        var response = await mediator.Send(command, ct);

        return Ok(response);
    }
}

#endregion

#region HANDLER

public sealed class GetBatchUuidHandler(
    AppDbContext dbContext,
    IPrintboxApi printboxApi,
    ILogger<GetBatchUuidHandler> logger)
    : IRequestHandler<GetBatchUuidCommand, SuccessResponse<string>>
{
    private readonly PrintboxSettings _printboxSettings =
        AppSettingsAnn.AppSettings.PrintboxSettings;

    public async Task<SuccessResponse<string>> Handle(
        GetBatchUuidCommand request,
        CancellationToken ct)
    {
        var entity = await dbContext.Batches
            .FirstOrDefaultAsync(x => x.Fo == request.Fo, ct);

        if (entity is null)
            throw new Exception("FO not found");

        var tokenResponse = await printboxApi.GetTokenAsync(
            new Dictionary<string, string>
            {
                ["client_id"] = _printboxSettings.ClientId,
                ["client_secret"] = _printboxSettings.ClientSecret,
                ["grant_type"] = "client_credentials"
            },
            ct);

        string bearer = $"Bearer {tokenResponse.AccessToken}";

        entity.Uuid = await ResolveUuidAsync(
            entity.Magento,
            bearer,
            ct);

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "UUID updated for FO {Fo}",
            request.Fo);

        return new SuccessResponse<string>(
            "Success",
            "UUID updated successfully");
    }

    private async Task<string> ResolveUuidAsync(
        string magento,
        string bearer,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(magento))
            return "";

        try
        {
            var order = await printboxApi.GetOrderAsync(
                $"b4com-{magento}",
                bearer,
                ct);

            return order?.Projects?
                .FirstOrDefault()?
                .Uuid ?? "";
        }
        catch
        {
            return "";
        }
    }
}

#endregion
