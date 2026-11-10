using ClosedXML.Excel;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Infrastructure.Persistence;

namespace Service.Ann.Batch.Api.Features.Batches;

#region COMMAND

public record EnrichWorkOrdersExcelCommand(
    IFormFile File
) : IRequest<byte[]>;

#endregion

#region VALIDATOR

public class EnrichWorkOrdersExcelValidator
    : AbstractValidator<EnrichWorkOrdersExcelCommand>
{
    public EnrichWorkOrdersExcelValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("Excel file is required.");
    }
}

#endregion

#region CONTROLLER

[ApiController]
[Route("batch")]
[Tags("Batch")]
public class EnrichWorkOrdersExcelController(
    IMediator mediator)
    : ControllerBase
{
    [HttpPost("enrich-work-orders")]
    public async Task<IActionResult> Upload(
        IFormFile file,
        CancellationToken ct)
    {
        var excel = await mediator.Send(
            new EnrichWorkOrdersExcelCommand(file),
            ct);

        return File(
            excel,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Active_work_orders_enriched_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }
}

#endregion

#region HANDLER

public sealed class EnrichWorkOrdersExcelHandler(
    AppDbContext dbContext,
    ILogger<EnrichWorkOrdersExcelHandler> logger)
    : IRequestHandler<
        EnrichWorkOrdersExcelCommand,
        byte[]>
{
    public async Task<byte[]> Handle(
        EnrichWorkOrdersExcelCommand request,
        CancellationToken ct)
    {
        try
        {
            using var stream =
                request.File.OpenReadStream();

            using var workbook =
                new XLWorkbook(stream);

            var worksheet =
                workbook.Worksheet(1);

            var lastRow =
                worksheet.LastRowUsed().RowNumber();

            var lastColumn =
                worksheet.LastColumnUsed().ColumnNumber();

            int projectNumberColumn = -1;

            // Buscar columna ProjectNumber
            for (int col = 1; col <= lastColumn; col++)
            {
                var header = worksheet
                    .Cell(1, col)
                    .GetString()
                    .Trim();

                if (header.Equals(
                    "ProjectNumber",
                    StringComparison.OrdinalIgnoreCase))
                {
                    projectNumberColumn = col;
                    break;
                }
            }

            if (projectNumberColumn == -1)
            {
                throw new Exception(
                    "Column 'ProjectNumber' not found.");
            }

            // Leer todos los PO
            var poList = new List<string>();

            for (int row = 2; row <= lastRow; row++)
            {
                var po = worksheet
                    .Cell(row, projectNumberColumn)
                    .GetString()
                    .Trim();

                if (!string.IsNullOrWhiteSpace(po))
                {
                    poList.Add(po);
                }
            }

            // Buscar todos los PO en una sola consulta
            var batches = await dbContext.Batches
                .AsNoTracking()
                .Where(x => poList.Contains(x.Fo))
                .OrderByDescending(x => x.BatchDate)
                .ToListAsync(ct);

            var lookup = batches
                .GroupBy(x => x.Fo)
                .ToDictionary(
                    x => x.Key,
                    x => x.First());

            // Agregar columnas nuevas
            int srColumn = lastColumn + 1;
            int approvalColumn = lastColumn + 2;
            int foundColumn = lastColumn + 3;

            worksheet.Cell(1, srColumn).Value = "SR";
            worksheet.Cell(1, approvalColumn).Value = "Approval Date";
            worksheet.Cell(1, foundColumn).Value = "Found";

            worksheet.Cell(1, srColumn)
                .Style.Font.Bold = true;

            worksheet.Cell(1, approvalColumn)
                .Style.Font.Bold = true;

            worksheet.Cell(1, foundColumn)
                .Style.Font.Bold = true;

            // Llenar resultados
            for (int row = 2; row <= lastRow; row++)
            {
                var po = worksheet
                    .Cell(row, projectNumberColumn)
                    .GetString()
                    .Trim();

                if (string.IsNullOrWhiteSpace(po))
                    continue;

                if (lookup.TryGetValue(po, out var batch))
                {
                    worksheet.Cell(row, foundColumn)
                        .Value = "Found";

                    worksheet.Cell(row, srColumn)
                        .Value = batch.Sr ?? "";

                    if (batch.Srapprdate.HasValue)
                    {
                        worksheet.Cell(row, approvalColumn)
                            .Value = batch.Srapprdate.Value;

                        worksheet.Cell(row, approvalColumn)
                            .Style.DateFormat.Format =
                            "MM/dd/yyyy";
                    }
                }
                else
                {
                    worksheet.Cell(row, foundColumn)
                        .Value = "Not Found";

                    worksheet.Cell(row, srColumn)
                        .Value = "";

                    worksheet.Cell(row, approvalColumn)
                        .Value = "";
                }
            }

            worksheet.Columns()
                .AdjustToContents();

            using var output =
                new MemoryStream();

            workbook.SaveAs(output);

            logger.LogInformation(
                "Work orders enriched successfully. Rows processed: {Count}",
                lastRow - 1);

            return output.ToArray();
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error enriching work orders Excel");

            throw;
        }
    }
}

#endregion