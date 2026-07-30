using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Application.Wrappers;
using Service.Ann.Batch.Api.Features.Batch;
using Service.Ann.Batch.Api.Infrastructure.Persistence;
using System.Text.Json;

namespace Service.Ann.Batch.Api.Features.Batches;

public record GetBatchAs400BulkCommand(List<string> Fos)
    : IRequest<SuccessResponse<string>>;

public sealed class GetBatchAs400BulkValidator
    : AbstractValidator<GetBatchAs400BulkCommand>
{
    public GetBatchAs400BulkValidator()
    {
        RuleFor(x => x.Fos)
            .NotEmpty()
            .WithMessage("Fos list is required");

        RuleForEach(x => x.Fos)
            .NotEmpty()
            .WithMessage("FO cannot be empty");
    }
}

[ApiController]
[Route("batch")]
[Tags("Batch")]
public class GetBatchAs400BulkController(IMediator mediator)
    : ControllerBase
{
    [HttpPost("get-as400-bulk")]
    public async Task<IActionResult> Post(
        [FromBody] GetBatchAs400BulkCommand command,
        CancellationToken ct)
    {
        var response = await mediator.Send(command, ct);
        return Ok(response);
    }
}
public sealed class GetBatchAs400BulkHandler(
    AppDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    ILogger<GetBatchAs400BulkHandler> logger)
    : IRequestHandler<GetBatchAs400BulkCommand, SuccessResponse<string>>
{
    private string _ApiKey = "gEIKqr1fWertQhg2Rm5LGzc3Qxl5uNoA_gEIKqr1fWertQhg2Rm5LGzc3Qxl5uNoA-SeG";

    public async Task<SuccessResponse<string>> Handle(
        GetBatchAs400BulkCommand request,
        CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", _ApiKey);

        int updated = 0;
        int notFound = 0;
        int failed = 0;

        foreach (var fo in request.Fos)
        {
            try
            {
                var entity = await dbContext.Batches
                    .FirstOrDefaultAsync(x => x.Fo == fo, ct);

                if (entity is null)
                {
                    notFound++;
                    continue;
                }

                var response = await client.GetAsync(
                    $"https://localhost:7057/announcements/{fo}",
                    ct);

                if (!response.IsSuccessStatusCode)
                {
                    failed++;
                    continue;
                }

                var json = await response.Content.ReadAsStringAsync(ct);

                var result = JsonSerializer.Deserialize<AnnAs400Response>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (result?.Data is null)
                {
                    failed++;
                    continue;
                }

                // ✅ actualizar entidad
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

                updated++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing FO {Fo}", fo);
                failed++;
            }
        }

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "AS400 Bulk completed | Updated: {Updated}, NotFound: {NotFound}, Failed: {Failed}",
            updated, notFound, failed);

        return new SuccessResponse<string>(
            "Success",
            $"Updated: {updated}, NotFound: {notFound}, Failed: {failed}");
    }
}