using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Application.Wrappers;
using Service.Ann.Batch.Api.Domain.Entities;
using Service.Ann.Batch.Api.Infrastructure.Persistence;

namespace Service.Ann.Batch.Api.Features.Batch;

#region 1. QUERY

/// <summary>
/// Query to retrieve batch records with filters
/// </summary>
/// <param name="StartDate" example="2026-05-12">Filter: Start date (>=)</param>
/// <param name="EndDate" example="2026-05-20">Filter: End date (<=)</param>
/// <param name="Status" example="SHIPPED">Filter by Status</param>
/// <param name="Po" example="123456">Filter by Purchase Order</param>
/// <param name="Fo" example="00012345">Filter by FO</param>
/// <param name="ShipMethod" example="FEDEX">Filter by ship method</param>
/// <param name="HasUuid" example="true">Has UUID (true/false)</param>
/// <param name="HasFiles" example="true">Has files (true/false)</param>
/// <param name="Item" example="GRETADLAB DGTL">Filter by item</param>
public record GetBatchesQuery(
    DateTime? StartDate,
    DateTime? EndDate,
    string? Status,
    string? Po,
    string? Fo,
    string? Sr,
    string? ShipMethod,
    bool? HasUuid,
    bool? Srapproved,
    bool? HasFiles,
    string? Item
) : IRequest<SuccessResponse<List<GetBatchesResponse>>>;

#endregion

#region 2. VALIDATOR

public sealed class GetBatchesValidator : AbstractValidator<GetBatchesQuery>
{
    public GetBatchesValidator()
    {
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate!)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("EndDate must be greater than or equal to StartDate.");
    }
}

#endregion

#region 3. AUTOMAPPER

public sealed class GetBatchesMappingProfile : Profile
{
    public GetBatchesMappingProfile()
    {
        CreateMap<BatchEntity, GetBatchesResponse>();
    }
}

#endregion

#region RESPONSE

/// <summary>
/// Batch record response
/// </summary>
public class GetBatchesResponse
{
    public Guid Id { get; set; }
    public DateTime BatchDate { get; set; }

    public string Fo { get; set; } = "";
    public string Item { get; set; } = "";
    public string School { get; set; } = "";

    public string StudentName { get; set; } = "";
    public string? StudentLastName { get; set; }

    public string Sr { get; set; } = "";
    public bool? Srapproved { get; set; }
    public DateTime? Srapprdate { get; set; }
    public string Magento { get; set; } = "";
    public string Po { get; set; } = "";

    public string Uuid { get; set; } = "";

    public string? ShipTracking { get; set; }
    public string? ShipMethod { get; set; }
    public DateTime? ShipDate { get; set; }


    public string? Files { get; set; }
    public DateTime? FilesDate { get; set; }

    public string? Priority { get; set; }
    public string? Status { get; set; }
    public int? Quantity { get; set; }
}

#endregion

#region 4. CONTROLLER

[ApiController]
[Route("batch")]
[Tags("Batch")]
public class GetBatchesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Retrieve batch records with filters
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(SuccessResponse<List<GetBatchesResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? status,
        [FromQuery] string? po,
        [FromQuery] string? fo,
        [FromQuery] string? sr,
        [FromQuery] string? shipMethod,
        [FromQuery] bool? hasUuid,
        [FromQuery] bool? srapproved,
        [FromQuery] bool? hasFiles,
        [FromQuery] string? item,
        CancellationToken ct)
    {
        var response = await mediator.Send(
            new GetBatchesQuery(
                startDate,
                endDate,
                status,
                po,
                fo,
                sr,
                shipMethod,
                hasUuid,
                srapproved,
                hasFiles,
                item
            ), ct);

        return Ok(response);
    }
}

#endregion

#region 5. HANDLER

public sealed class GetBatchesHandler(
    AppDbContext dbContext,
    IMapper mapper,
    ILogger<GetBatchesHandler> logger)
    : IRequestHandler<GetBatchesQuery, SuccessResponse<List<GetBatchesResponse>>>
{
    public async Task<SuccessResponse<List<GetBatchesResponse>>> Handle(
        GetBatchesQuery request,
        CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Fetching batches with filters");

            var query = dbContext.Batches
                .AsNoTracking()
                .AsQueryable();

            if (request.StartDate.HasValue)
                query = query.Where(x => x.BatchDate >= request.StartDate.Value);

            if (request.EndDate.HasValue)
                query = query.Where(x => x.BatchDate <= request.EndDate.Value);

            if (!string.IsNullOrWhiteSpace(request.Status))
                query = query.Where(x => x.Status == request.Status);

            if (!string.IsNullOrWhiteSpace(request.Po))
                query = query.Where(x => x.Po == request.Po);

            if (!string.IsNullOrWhiteSpace(request.Fo))
                query = query.Where(x => x.Fo.Contains(request.Fo));

            if (!string.IsNullOrWhiteSpace(request.Sr))
                query = query.Where(x => x.Sr == request.Sr);

            if (!string.IsNullOrWhiteSpace(request.ShipMethod))
                query = query.Where(x => x.ShipMethod == request.ShipMethod);

            if (request.HasUuid.HasValue)
                query = request.HasUuid.Value
                    ? query.Where(x => !string.IsNullOrEmpty(x.Uuid))
                    : query.Where(x => string.IsNullOrEmpty(x.Uuid));

            if (request.HasFiles.HasValue)
                query = request.HasFiles.Value
                    ? query.Where(x => !string.IsNullOrEmpty(x.Files))
                    : query.Where(x => string.IsNullOrEmpty(x.Files));

            if (request.Srapproved.HasValue)
                query = query.Where(x => x.Srapproved == request.Srapproved.Value);

            if (!string.IsNullOrWhiteSpace(request.Item))
                query = query.Where(x => x.Item.Contains(request.Item));

            var data = await query
                .OrderByDescending(x => x.BatchDate)
                .ToListAsync(ct);

            var result = mapper.Map<List<GetBatchesResponse>>(data);

            return new SuccessResponse<List<GetBatchesResponse>>(
                result,
                "Batch records retrieved successfully"
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching batches");
            throw;
        }
    }
}

#endregion
