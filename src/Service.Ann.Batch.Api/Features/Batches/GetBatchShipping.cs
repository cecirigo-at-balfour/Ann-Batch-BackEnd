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
using System.Text.Json.Serialization;

namespace Service.Ann.Batch.Api.Features.Batch;

#region COMMAND

public record GetBatchShippingCommand(string Fo)
    : IRequest<SuccessResponse<string>>;

#endregion

#region VALIDATOR

public sealed class GetBatchShippingValidator
    : AbstractValidator<GetBatchShippingCommand>
{
    public GetBatchShippingValidator()
    {
        RuleFor(x => x.Fo)
            .NotEmpty();
    }
}

#endregion

#region RESPONSE DTO

public class AnnouncementResponse
{
    [JsonPropertyName("succeeded")]
    public bool Succeeded { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("data")]
    public List<ShippingDto>? Data { get; set; }
}
#endregion

#region CONTROLLER
[ApiController]
[Route("batch")]
[Tags("Batch")]
public class GetBatchShippingController(IMediator mediator)
    : ControllerBase
{
    [HttpPost("get-shipping")]
    public async Task<IActionResult> Post(
        [FromBody] GetBatchShippingCommand command,
        CancellationToken ct)
    {
        var response = await mediator.Send(command, ct);

        return Ok(response);
    }
}

#endregion

#region HANDLER

public sealed class GetBatchShippingHandler(
    AppDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    ILogger<GetBatchShippingHandler> logger,
    IOptions<ApiSettings> apiSettings)
    : IRequestHandler<GetBatchShippingCommand, SuccessResponse<string>>
{
    private string _ApiKey = "gEIKqr1fWertQhg2Rm5LGzc3Qxl5uNoA_gEIKqr1fWertQhg2Rm5LGzc3Qxl5uNoA-SeG";
    private readonly string _AS400BaseUrl = apiSettings.Value.AS400BaseUrl;
    public async Task<SuccessResponse<string>> Handle(
        GetBatchShippingCommand request,
        CancellationToken ct)
    {
        var entity = await dbContext.Batches
            .FirstOrDefaultAsync(x => x.Fo == request.Fo, ct);

        if (entity is null)
            throw new Exception("FO not found");

        var client = httpClientFactory.CreateClient();

        client.DefaultRequestHeaders.Add("x-api-key", _ApiKey);

        var response = await client.GetAsync(
            $"{_AS400BaseUrl}/shipping-detail/{request.Fo}",
            ct);
        Console.WriteLine(response);
        if (!response.IsSuccessStatusCode)
            throw new Exception("Announcement API failed");

        var json = await response.Content.ReadAsStringAsync(ct);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new NullableDateTimeConverter());

        var result = JsonSerializer.Deserialize<AnnouncementResponse>(json, options);

        if (result?.Data == null || !result.Data.Any())
            throw new Exception("No shipping data found");

        var items = result.Data;

        // ✅ Puedes tomar un "base" (ya que dirección y persona son iguales)
        var first = items.First();

        // ✅ Combinar múltiples items
        var validItems = new[]
        { "GRETADLAB","GPHOTO","GDGTL"};

        entity.Item = string.Join(", ",
            items
                .Where(x => validItems.Any(v =>
                    x.ItemCode.Contains(v, StringComparison.OrdinalIgnoreCase)))
                .Select(x => x.ItemCode));

        // ✅ Tracking único (por si hay duplicados)
        entity.ShipTracking = items
            .Select(x => x.TrackingNumber)
            .Distinct()
            .FirstOrDefault();

        // ✅ Datos comunes
        entity.ShipAddress = first.AddressLine1;
        entity.ShipAddress2 = first.AddressLine2;
        entity.ShipAddress3 = first.AddressLine3;

        entity.School = first.SchoolName;
        entity.ShipCity = first.City;
        entity.ShipState = first.State;
        entity.ShipCode = first.ZipCode;

        entity.ShipDate = first.ShippingDt;
        entity.CustomerTrackingNumber = first.CustomerTrackingNumber;

        // ✅ Opcional: nombre completo
        entity.StudentName = first.FullName;
        entity.StudentLastName = first.LastName;

        Console.WriteLine(entity);

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
