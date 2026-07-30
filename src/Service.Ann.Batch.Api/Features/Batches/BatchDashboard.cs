using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Domain.Dtos.Baan;
using Service.Ann.Batch.Api.Infrastructure.Persistence;

namespace Service.Ann.Batch.Api.Features.Batches;

public record GetBatchDashboardQuery(
    DateTime FromDate,
    DateTime ToDate
) : IRequest<BatchDashboardResponse>;


public class BatchDashboardResponse
{
    public List<BatchDashboardItemDto> Batched { get; set; } = new();

    public int Total { get; set; }
    public int Shipped { get; set; }
    public int Cancelled { get; set; }
    public int Backordered { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}


public class GetBatchDashboardValidator
    : AbstractValidator<GetBatchDashboardQuery>
{
    public GetBatchDashboardValidator()
    {
        RuleFor(x => x.FromDate)
            .NotEmpty();

        RuleFor(x => x.ToDate)
            .NotEmpty()
            .GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("ToDate must be greater or equal than FromDate");
    }
}


[ApiController]
[Route("batch/dashboard")]
[Tags("Batch")]
public class GetBatchDashboardController(IMediator mediator)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetBatchDashboardQuery(fromDate, toDate), ct);

        return Ok(result);
    }
}

public class GetBatchDashboardHandler(
    AppDbContext dbContext,
    ILogger<GetBatchDashboardHandler> logger)
    : IRequestHandler<GetBatchDashboardQuery, BatchDashboardResponse>
{
    public async Task<BatchDashboardResponse> Handle(
        GetBatchDashboardQuery request,
        CancellationToken ct)
    {
        // ✅ Query base filtrado por rango
        var query = dbContext.Batches
            .Where(x => x.BatchDate >= request.FromDate &&
                        x.BatchDate <= request.ToDate)
                .OrderByDescending(x => x.BatchDate); 

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

              // ✅ lista completa para tabla
        var batched = await query
            .Select(x => new BatchDashboardItemDto
            {
                Id = x.Id,
                BatchDate = x.BatchDate,
                Fo = x.Fo,
                Item = x.Item,
                School = x.School,
                StudentName = x.StudentName,
                StudentLastName = x.StudentLastName,
                Sr = x.Sr,
                Magento = x.Magento,
                Po = x.Po,
                Uuid = x.Uuid,
                CustomerTrackingNumber = x.CustomerTrackingNumber,
                ShipTracking = x.ShipTracking,
                ShipMethod = x.ShipMethod,
                ShipDate = x.ShipDate,
                AddressName = x.AddressName,
                ShipAddress = x.ShipAddress,
                ShipAddress2 = x.ShipAddress2,
                ShipAddress3 = x.ShipAddress3,
                ShipCity = x.ShipCity,
                ShipState = x.ShipState,
                ShipCode = x.ShipCode,
                DeliveryDate = x.DeliveryDate,
                BookDate = x.BookDate,
                Files = x.Files,
                FilesDate = x.FilesDate,
                Priority = x.Priority,
                LineStatus = x.LineStatus,
                Status = x.Status,
                OrderTotal = x.OrderTotal,
                Payments = x.Payments,
                BalanceDue = x.BalanceDue,
                CreatedAt = x.CreatedAt,
                ModifiedAt = x.ModifiedAt,
                IsDeleted = x.IsDeleted,
                DeletedAt = x.DeletedAt
            })
            .ToListAsync(ct);

        // ✅ métricas (sobre el MISMO rango)
        int total = batched.Count;

        int shipped = batched.Count(x =>
            x.Status != null &&
            x.Status.Equals("shipped", StringComparison.OrdinalIgnoreCase));

        int cancelled = batched.Count(x =>
            x.Status != null &&
           (x.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase) ||
            x.Status.Equals("returned", StringComparison.OrdinalIgnoreCase)));

        int backordered = batched.Count(x =>
            x.Status != null &&
            x.Status.Equals("backordered", StringComparison.OrdinalIgnoreCase));

        logger.LogInformation(
            "Dashboard generated from {From} to {To} | Total: {Total}",
            request.FromDate,
            request.ToDate,
            total);

       

        return new BatchDashboardResponse
        {
            Batched = batched,
            Total = total,
            Shipped = shipped,
            Cancelled = cancelled,
            Backordered = backordered,
            FromDate = request.FromDate,
            ToDate = request.ToDate
        };
    }
}