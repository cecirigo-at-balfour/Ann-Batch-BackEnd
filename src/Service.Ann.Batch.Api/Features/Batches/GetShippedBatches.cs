using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Application.Wrappers;
using Service.Ann.Batch.Api.Domain.Entities;
using Service.Ann.Batch.Api.Infrastructure.Persistence;

namespace Service.Ann.Batch.Api.Features.Batches;

public record GetShippedBatchesQuery(
    DateTime? StartShipDate,
    DateTime? EndShipDate,
    string? Fo
) : IRequest<SuccessResponse<List<GetShippedBatchesResponse>>>;

public sealed class GetShippedBatchesValidator : AbstractValidator<GetShippedBatchesQuery>
{
    public GetShippedBatchesValidator()
    {
        RuleFor(x => x.EndShipDate)
            .GreaterThanOrEqualTo(x => x.StartShipDate!)
            .When(x =>
                x.StartShipDate.HasValue &&
                x.EndShipDate.HasValue);
    }
}

public class GetShippedBatchesResponse
{
    public Guid Id { get; set; }

    public string Fo { get; set; } = "";

    public string School { get; set; } = "";

    public string StudentName { get; set; } = "";

    public string Item { get; set; } = "";

    public string Po { get; set; } = "";

    public string ShipTracking { get; set; } = "";

    public string ShipMethod { get; set; } = "";

    public DateTime? ShipDate { get; set; }

    public string? Status { get; set; }

    public string? ShipAddress { get; set; }

    public string? ShipCity { get; set; }

    public string? ShipState { get; set; }

    public string? ShipCode { get; set; }

    public decimal? OrderTotal { get; set; }
}

public sealed class GetShippedBatchesMappingProfile
    : Profile
{
    public GetShippedBatchesMappingProfile()
    {
        CreateMap<BatchEntity, GetShippedBatchesResponse>();
    }
}


[ApiController]
[Route("reports")]
[Tags("Reports")]
public class GetShippedBatchesController(
    IMediator mediator)
    : ControllerBase
{
    [HttpGet("shipped-orders")]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime? startShipDate,
        [FromQuery] DateTime? endShipDate,
        [FromQuery] string? fo,
        CancellationToken ct)
    {
        var response = await mediator.Send(
            new GetShippedBatchesQuery(
                startShipDate,
                endShipDate,
                fo),
            ct);

        return Ok(response);
    }
}

public sealed class GetShippedBatchesHandler(
    AppDbContext dbContext,
    IMapper mapper,
    ILogger<GetShippedBatchesHandler> logger)
    : IRequestHandler<
        GetShippedBatchesQuery,
        SuccessResponse<List<GetShippedBatchesResponse>>>
{
    public async Task<
        SuccessResponse<List<GetShippedBatchesResponse>>>
        Handle(
            GetShippedBatchesQuery request,
            CancellationToken ct)
    {
        logger.LogInformation(
            "Fetching shipped orders");

        var query = dbContext.Batches
            .AsNoTracking()
            .Where(x => x.Status == "Shipped");

        if (!string.IsNullOrWhiteSpace(request.Fo))
        {
            query = query.Where(x => x.Fo == request.Fo);
        }
        else
        {
            if (request.StartShipDate.HasValue)
            {
                var start =
                    request.StartShipDate.Value.Date;

                query = query.Where(
                    x => x.ShipDate >= start);
            }

            if (request.EndShipDate.HasValue)
            {
                var end =
                    request.EndShipDate.Value.Date.AddDays(1);

                query = query.Where(
                    x => x.ShipDate < end);
            }
        }

        var data = await query
            .OrderByDescending(x => x.ShipDate)
            .ToListAsync(ct);

        var result =
            mapper.Map<List<GetShippedBatchesResponse>>(data);

        return new SuccessResponse<
            List<GetShippedBatchesResponse>>(
            result,
            $"{result.Count} shipped orders found");
    }
}



