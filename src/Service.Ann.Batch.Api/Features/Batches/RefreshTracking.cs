using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Service.Ann.Batch.Api.Application.Wrappers;
using Service.Ann.Batch.Api.Domain.Dtos.As400;
using Service.Ann.Batch.Api.Infrastructure.Configuration;
using Service.Ann.Batch.Api.Infrastructure.Persistence;
using System.Globalization;
using System.Text.Json;

namespace Service.Ann.Batch.Api.Features.Batches;

public record RefreshTrackingCommand()
    : IRequest<SuccessResponse<string>>;

[ApiController]
[Route("refresh-tracking")]
public class RefreshTrackingController(IMediator mediator)
    : ControllerBase
{
    [HttpPost("batch")]
    public async Task<IActionResult> Post(
        CancellationToken ct)
    {
        var response = await mediator.Send(
            new RefreshTrackingCommand(),
            ct);

        return Ok(response);
    }
}

public class AnnouncementResponse
{
    public bool Succeeded { get; set; }

    public string Message { get; set; } = "";

    public OrderDto? Data { get; set; }
}

public sealed class RefreshTrackingHandler(
    AppDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    ILogger<RefreshTrackingHandler> logger,
    IOptions<ApiSettings> apiSettings)
    : IRequestHandler<RefreshTrackingCommand, SuccessResponse<string>>
{
    private readonly string _AS400BaseUrl = apiSettings.Value.AS400BaseUrl;
    public async Task<SuccessResponse<string>> Handle(
        RefreshTrackingCommand request,
        CancellationToken ct)
    {
       
    var batches = await dbContext.Batches
    //   .Where(x => x.ShipTracking == null || x.ShipTracking == "")
  //  .Where(x => x.ShipDate == null)
      .ToListAsync(ct);

        var client = httpClientFactory.CreateClient();

        int updated = 0;

        foreach (var batch in batches)
        {
            try
            {
                //  if (!string.IsNullOrWhiteSpace(batch.ShipTracking))               
               //     continue;

                var httpRequest = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{_AS400BaseUrl}/{batch.Fo}");

                httpRequest.Headers.Add(
                    "x-api-key",
                    "gEIKqr1fWertQhg2Rm5LGzc3Qxl5uNoA_gEIKqr1fWertQhg2Rm5LGzc3Qxl5uNoA-SeG");

                var response =
                    await client.SendAsync(httpRequest, ct);

                if (!response.IsSuccessStatusCode)
                    continue;

                var json =
                    await response.Content.ReadAsStringAsync(ct);

                var result =
                    JsonSerializer.Deserialize<AnnouncementResponse>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (result?.Data == null)
                    continue;

                batch.ShipTracking = result.Data.ShipTrackingNum;
                batch.ShipMethod = result.Data.ShippingMthd;
                batch.ShipDate = result.Data.ShipDt;
                Console.WriteLine(batch);
                Console.WriteLine(result.Data);

                dbContext.Batches.Update(batch);

                updated++;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error processing FO {Fo}",
                    batch.Fo);
            }
        }

        await dbContext.SaveChangesAsync(ct);

        return new SuccessResponse<string>(
            "Success",
            $"{updated} tracking numbers updated");
    }

  }

