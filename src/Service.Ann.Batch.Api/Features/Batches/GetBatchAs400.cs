
using FluentValidation;
using global::Service.Ann.Batch.Api.Application.Wrappers;
using global::Service.Ann.Batch.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Domain.Dtos.As400;
using System.Text.Json;

namespace Service.Ann.Batch.Api.Features.Batch;

#region COMMAND

public record GetBatchAs400Command(string Fo)
    : IRequest<SuccessResponse<string>>;

#endregion

#region VALIDATOR

public sealed class GetBatchAs400Validator
    : AbstractValidator<GetBatchAs400Command>
{
    public GetBatchAs400Validator()
    {
        RuleFor(x => x.Fo)
            .NotEmpty();
    }
}

#endregion

#region RESPONSE DTO

public class AnnAs400Response
{
    public bool Succeeded { get; set; }

    public string Message { get; set; } = "";

    public OrderDto? Data { get; set; }
}
#endregion

#region CONTROLLER

[ApiController]
[Route("batch")]
[Tags("Batch")]
public class GetBatchAs400Controller(IMediator mediator)
    : ControllerBase
{
    [HttpPost("get-as400")]
    public async Task<IActionResult> Post(
        [FromBody] GetBatchAs400Command command,
        CancellationToken ct)
    {
        var response = await mediator.Send(command, ct);

        return Ok(response);
    }
}

#endregion

#region HANDLER

public sealed class GetBatchAs400Handler(
    AppDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    ILogger<GetBatchAs400Handler> logger)
    : IRequestHandler<GetBatchAs400Command, SuccessResponse<string>>
{
    private string _ApiKey = "gEIKqr1fWertQhg2Rm5LGzc3Qxl5uNoA_gEIKqr1fWertQhg2Rm5LGzc3Qxl5uNoA-SeG";
    public async Task<SuccessResponse<string>> Handle(
        GetBatchAs400Command request,
        CancellationToken ct)
    {
        var entity = await dbContext.Batches
            .FirstOrDefaultAsync(x => x.Fo == request.Fo, ct);

        if (entity is null)
            throw new Exception("FO not found");

        var client = httpClientFactory.CreateClient();

        client.DefaultRequestHeaders.Add("x-api-key", _ApiKey);

        var response = await client.GetAsync(
            $"https://localhost:7057/announcements/{request.Fo}",
            ct);
        Console.WriteLine(response);
        if (!response.IsSuccessStatusCode)
            throw new Exception("Announcement API failed");

        var json = await response.Content.ReadAsStringAsync(ct);

        var result = JsonSerializer.Deserialize<AnnAs400Response>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (result?.Data is null)
            throw new Exception("No shipping data found");

        entity.ShipTracking = result.Data.ShipTrackingNum;
        entity.ShipMethod = result.Data.ShippingMthd;           
        entity.ShipDate = result.Data.ShippingDt;    

        entity.Status = result.Data.OrdStsDescription;
        entity.LineStatus = result.Data.LineStatusDescription;

        entity.StudentName = result.Data.FirstName;
        entity.StudentLastName = result.Data.LastName;

        entity.OrderTotal = result.Data.OrderTotal;
        entity.Payments = result.Data.Payments;
        entity.BalanceDue = result.Data.BalanceDue;       

        entity.BookDate = result.Data.BookDt;
        entity.ShipDate = result.Data.ShipDt;
        entity.DeliveryDate = result.Data.DeliveryDt;

        Console.WriteLine(result.Data.ShipDt);

        dbContext.Batches.Update(entity);

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "Shipping updated for FO {Fo}",
            request.Fo);

        return new SuccessResponse<string>(
            "Success",
            "Shipping updated successfully");
    }
}

#endregion
