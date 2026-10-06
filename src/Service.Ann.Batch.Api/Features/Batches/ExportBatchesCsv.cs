using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Infrastructure.Persistence;
using System.Text;


namespace Service.Ann.Batch.Api.Features.Batches;

public record ExportBatchesCsvQuery(
    DateTime? StartDate,
    DateTime? EndDate,
    string? Status,
    string? Po,
    string? Fo,
    string? ShipTracking,
    string? ShipMethod,
    bool? HasUuid,
    string? Priority,
    bool? HasFiles,
    string? Item
) : IRequest<byte[]>;

public sealed class ExportBatchesCsvValidator
    : AbstractValidator<ExportBatchesCsvQuery>
{
    public ExportBatchesCsvValidator()
    {
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate!)
            .When(x => x.StartDate.HasValue &&
                       x.EndDate.HasValue);
    }
}

[ApiController]
[Route("batch")]
[Tags("Batch")]
public class ExportBatchesCsvController(
    IMediator mediator)
    : ControllerBase
{
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? status,
        [FromQuery] string? po,
        [FromQuery] string? fo,
        [FromQuery] string? shipTracking,
        [FromQuery] string? shipMethod,
        [FromQuery] bool? hasUuid,
        [FromQuery] string? priority,
        [FromQuery] bool? hasFiles,
        [FromQuery] string? item,
        CancellationToken ct)
    {
        var file = await mediator.Send(
            new ExportBatchesCsvQuery(
                startDate,
                endDate,
                status,
                po,
                fo,
                shipTracking,
                shipMethod,
                hasUuid,
                priority,
                hasFiles,
                item),
            ct);

        return File(
            file,
            "text/csv",
            $"batches_{DateTime.Now:yyyyMMddHHmmss}.csv");
    }
}


public sealed class ExportBatchesCsvHandler(
    AppDbContext dbContext,
    ILogger<ExportBatchesCsvHandler> logger)
    : IRequestHandler<ExportBatchesCsvQuery, byte[]>
{
    public async Task<byte[]> Handle(
        ExportBatchesCsvQuery request,
        CancellationToken ct)
    {
        try
        {
            var query = dbContext.Batches
                .AsNoTracking()
                .AsQueryable();

            if (request.StartDate.HasValue)
                query = query.Where(x =>
                    x.BatchDate >= request.StartDate.Value);

            if (request.EndDate.HasValue)
                query = query.Where(x =>
                    x.BatchDate <= request.EndDate.Value);

            if (!string.IsNullOrWhiteSpace(request.Status))
                query = query.Where(x =>
                    x.Status == request.Status);

            if (!string.IsNullOrWhiteSpace(request.Po))
                query = query.Where(x =>
                    x.Po == request.Po);

            if (!string.IsNullOrWhiteSpace(request.Fo))
                query = query.Where(x =>
                    x.Fo == request.Fo);

            if (!string.IsNullOrWhiteSpace(request.ShipTracking))
                query = query.Where(x =>
                    x.ShipTracking == request.ShipTracking);

            if (!string.IsNullOrWhiteSpace(request.ShipMethod))
                query = query.Where(x =>
                    x.ShipMethod == request.ShipMethod);

            if (request.HasUuid.HasValue)
                query = request.HasUuid.Value
                    ? query.Where(x =>
                        !string.IsNullOrEmpty(x.Uuid))
                    : query.Where(x =>
                        string.IsNullOrEmpty(x.Uuid));

            if (request.HasFiles.HasValue)
                query = request.HasFiles.Value
                    ? query.Where(x =>
                        !string.IsNullOrEmpty(x.Files))
                    : query.Where(x =>
                        string.IsNullOrEmpty(x.Files));

            if (!string.IsNullOrWhiteSpace(request.Priority))
                query = query.Where(x =>
                    x.Priority == request.Priority);

            if (!string.IsNullOrWhiteSpace(request.Item))
                query = query.Where(x =>
                    x.Item == request.Item);

            var data = await query
                .OrderByDescending(x => x.BatchDate)
                .ToListAsync(ct);

            var csv = new StringBuilder();

            csv.AppendLine(
                "Date,BatchDate,Fo,Status,Item,Quantity,Files,FilesDate,Sr,Approved,App Date,School,StudentName,Magento,Po,ShipTracking,Uuid");

            foreach (var itemRow in data)
            {
                csv.AppendLine(
                    $"{itemRow.BookDate:yyyy-MM-dd}," +
                    $"{itemRow.BatchDate:yyyy-MM-dd}," +
                    $"\"{CsvValue(itemRow.Fo)}\"," +
                    $"\"{CsvValue(itemRow.Status)}\"," +
                    $"\"{CsvValue(itemRow.Item)}\"," +
                    $"{itemRow.Quantity}," +
                    $"\"{CsvValue(itemRow.Files)}\"," +
                    $"{itemRow.FilesDate:yyyy-MM-dd}," +
                    $"\"{CsvValue(itemRow.Sr)}\"," +
                    $"{itemRow.Srapproved}," +
                    $"{itemRow.Srapprdate:yyyy-MM-dd}," +
                    $"\"{CsvValue(itemRow.School)}\"," +
                    $"\"{CsvValue(itemRow.StudentName)}\"," +
                    $"\"{CsvValue(itemRow.Magento)}\"," +
                    $"\"{CsvValue(itemRow.Po)}\"," +
                    $"\"{CsvValue(itemRow.ShipTracking)}\"," +
                    $"\"{CsvValue(itemRow.Uuid)}\"");
            }

            return Encoding.UTF8.GetBytes(
                csv.ToString());
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error exporting batches");

            throw;
        }
    }
    private static string CsvValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return value
            .Replace("\"", "\"\"")
            .Replace("\r", " ")
            .Replace("\n", " ");
    }
}

