using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Service.Ann.Batch.Api.Application.Wrappers;
using Service.Ann.Batch.Api.Converters;
using Service.Ann.Batch.Api.Domain.Dtos.As400;
using Service.Ann.Batch.Api.Infrastructure.Configuration;
using Service.Ann.Batch.Api.Infrastructure.Persistence;
using System.Text.Json;

namespace Service.Ann.Batch.Api.Features.Batches;

public class BatchResponse
{
    public bool Succeeded { get; set; }
    public string Message { get; set; } = "";
    public List<ShippingDto>? Data { get; set; }
}

public record UpdateBatchShippingBulkCommand(
    List<string> fos
) : IRequest<SuccessResponse<string>>;

public class UpdateBatchShippingBulkValidator
    : AbstractValidator<UpdateBatchShippingBulkCommand>
{
    public UpdateBatchShippingBulkValidator()
    {
        RuleFor(x => x.fos)
            .NotEmpty()
            .WithMessage("At least one FO is required");

        RuleForEach(x => x.fos)

                .Must(fo =>
                {
                    Console.WriteLine($"FO recibido: '{fo}'");
                    return !string.IsNullOrWhiteSpace(fo);
                })

            .NotEmpty();
    }
}

[ApiController]
[Route("batch")]
[Tags("Batch")]
public class UpdateBatchShippingBulkController(IMediator mediator)
    : ControllerBase
{
    [HttpPost("update-shipping-bulk")]
    public async Task<IActionResult> Post(
        [FromBody] UpdateBatchShippingBulkCommand command,
        CancellationToken ct)
    {

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await mediator.Send(command, ct);
        return Ok(response);
    }
}

public sealed class UpdateBatchShippingBulkHandler(
    AppDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    ILogger<UpdateBatchShippingBulkHandler> logger,
    IOptions<ApiSettings> apiSettings)
    : IRequestHandler<UpdateBatchShippingBulkCommand, SuccessResponse<string>>
{
    private const string ApiKey = "gEIKqr1fWertQhg2Rm5LGzc3Qxl5uNoA_gEIKqr1fWertQhg2Rm5LGzc3Qxl5uNoA-SeG";
    private readonly string _AS400BaseUrl = apiSettings.Value.AS400BaseUrl;

    public async Task<SuccessResponse<string>> Handle(
        UpdateBatchShippingBulkCommand request,
        CancellationToken ct)
    {
       
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", ApiKey);
        

        int successCount = 0;
        var errors = new List<string>();

        foreach (var fo in request.fos)
        {
            try
            {
                var entity = await dbContext.Batches
                    .FirstOrDefaultAsync(x => x.Fo == fo, ct);

                if (entity is null)
                {
                    errors.Add($"FO {fo} not found");
                    continue;
                }

                
                var response = await client.GetAsync(
                    $"{_AS400BaseUrl}/shipping-detail/{fo}",
                    ct);

                Console.WriteLine(response);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine(response);
                    errors.Add($"API failed for FO {fo}");
                    continue;
                }

                var json = await response.Content.ReadAsStringAsync(ct);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                options.Converters.Add(new NullableDateTimeConverter());

                var result = JsonSerializer.Deserialize<BatchResponse>(json, options);

                if (result?.Data == null || !result.Data.Any())
                {
                    errors.Add($"No data for FO {fo}");
                    continue;
                }

                var items = result.Data;
                var first = items.First();

                entity.Item = string.Join(", ", items.Select(x => x.ItemCode));
                entity.ShipTracking = items
                    .Select(x => x.TrackingNumber)
                    .Distinct()
                    .FirstOrDefault();

                entity.ShipAddress = first.AddressLine1;
                entity.ShipAddress2 = first.AddressLine2;
                entity.ShipAddress3 = first.AddressLine3;

                entity.School = first.SchoolName;
                entity.ShipCity = first.City;
                entity.ShipState = first.State;
                entity.ShipCode = first.ZipCode;

                entity.ShipDate = first.ShippingDt;
                entity.CustomerTrackingNumber = first.CustomerTrackingNumber;

                entity.StudentName = first.FullName;
                entity.StudentLastName = first.LastName;

                successCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing FO {Fo}", fo);
                errors.Add($"Error on FO {fo}");
            }
        }

        // ✅ Guardar todos juntos (más eficiente)
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "Bulk shipping update completed. Success: {Success}, Errors: {Errors}",
            successCount, errors.Count);

        return new SuccessResponse<string>(
            "Completed",
            $"Success: {successCount}, Errors: {errors.Count}");
    }
}