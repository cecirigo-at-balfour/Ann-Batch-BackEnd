using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Application.Wrappers;
using Service.Ann.Batch.Api.Domain.Entities;
using Service.Ann.Batch.Api.Infrastructure.Persistence;

namespace Service.Ann.Batch.Api.Features.Batches;

#region 1. QUERY

/// <summary>
/// Query to retrieve a batch record by Id
/// </summary>
/// <param name="Id" example="d290f1ee-6c54-4b01-90e6-d701748f0851">
/// Batch unique identifier
/// </param>
public record GetBatchByIdQuery(Guid Id)
    : IRequest<SuccessResponse<GetBatchByIdResponse>>;

#endregion

#region 2. VALIDATOR

public sealed class GetBatchByIdValidator : AbstractValidator<GetBatchByIdQuery>
{
    public GetBatchByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id is required.");
    }
}

#endregion

#region 3. AUTOMAPPER

public sealed class GetBatchByIdMappingProfile : Profile
{
    public GetBatchByIdMappingProfile()
    {
        CreateMap<BatchEntity, GetBatchByIdResponse>();
    }
}

#endregion

#region RESPONSE
/// <summary>
/// Batch record response by Id
/// </summary>
public class GetBatchByIdResponse
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

    public string? AddressName { get; set; }
    public string? ShipAddress { get; set; }
    public string? ShipAddress2 { get; set; }
    public string? ShipAddress3 { get; set; }

    public string? ShipCity { get; set; }
    public string? ShipState { get; set; }
    public string? ShipCode { get; set; }

    public decimal? OrderTotal { get; set; }
    public decimal? Payments { get; set; }
    public decimal? BalanceDue { get; set; }

    public DateTime? DeliveryDate { get; set; }
    public DateTime? BookDate { get; set; }
    public string? Files { get; set; }
    public DateTime? FilesDate { get; set; }

    public string? Priority { get; set; }
    public string? Status { get; set; }
    public int? quantity { get; set; }
}

#endregion

#region 4. CONTROLLER

[ApiController]
[Route("batch")]
[Tags("Batch")]
public class GetBatchByIdController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Get batch record by Id
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SuccessResponse<GetBatchByIdResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var response = await mediator.Send(
            new GetBatchByIdQuery(id), ct);

        return Ok(response);
    }
}

#endregion

#region 5. HANDLER

public sealed class GetBatchByIdHandler(
    AppDbContext dbContext,
    IMapper mapper,
    ILogger<GetBatchByIdHandler> logger)
    : IRequestHandler<GetBatchByIdQuery, SuccessResponse<GetBatchByIdResponse>>
{
    public async Task<SuccessResponse<GetBatchByIdResponse>> Handle(
        GetBatchByIdQuery request,
        CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Fetching batch by Id: {Id}", request.Id);

            var entity = await dbContext.Batches
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

            if (entity is null)
            {
                logger.LogWarning("Batch with Id {Id} not found", request.Id);

                return new SuccessResponse<GetBatchByIdResponse>(
                    data: null,
                    message: "Batch not found");
            }

            var result = mapper.Map<GetBatchByIdResponse>(entity);

            return new SuccessResponse<GetBatchByIdResponse>(
                result,
                "Batch retrieved successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching batch by Id");
            throw;
        }
    }
}

#endregion
