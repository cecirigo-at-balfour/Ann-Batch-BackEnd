using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Application.Wrappers;
using Service.Ann.Batch.Api.Common.Settings;
using Service.Ann.Batch.Api.Domain.Dtos.Baan;
using Service.Ann.Batch.Api.Domain.Entities;
using Service.Ann.Batch.Api.Infrastructure.DataAccess.Baan;
using Service.Ann.Batch.Api.Infrastructure.Persistence;
using Service.Ann.Batch.Api.Infrastructure.Services;

namespace Service.Ann.Batch.Api.Features.Batches;

#region 1. COMMAND

public record ProcessBatchFromCsvCommand(
    IFormFile File,
    DateTime BatchDate
) : IRequest<SuccessResponse<string>>;

#endregion

#region 2. VALIDATOR

public sealed class ProcessBatchFromCsvValidator : AbstractValidator<ProcessBatchFromCsvCommand>
{
    public ProcessBatchFromCsvValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("File is required.")
            .Must(f => f.Length > 0)
            .WithMessage("File is empty.")
            .Must(f => Path.GetExtension(f.FileName).ToLower() == ".csv")
            .WithMessage("Only .csv files are allowed.");

        RuleFor(x => x.BatchDate)
            .NotEmpty();
    }
}

#endregion

#region 3. AUTOMAPPER

public sealed class BatchMappingProfile : Profile
{
    public BatchMappingProfile()
    {
        CreateMap<BaanBatchDto, BatchEntity>();
    }
}

#endregion

#region 4. CONTROLLER

[ApiController]
[Route("batch")]
[Tags("Batch")]
public class ProcessBatchFromCsvController(IMediator mediator) : ControllerBase
{
    [HttpPost("process-csv")]
    public async Task<IActionResult> Post(
        IFormFile file,
        [FromForm] DateTime batchDate,
        CancellationToken ct)
    {
        var response = await mediator.Send(
            new ProcessBatchFromCsvCommand(file, batchDate), ct);

        return Ok(response);
    }
}

#endregion

#region 5. HANDLER

public sealed class ProcessBatchFromCsvHandler(
    AppDbContext dbContext,
    IBaanRepository baanRepository,
    IPrintboxApi printboxApi,
    IMapper mapper,
    ILogger<ProcessBatchFromCsvHandler> logger,
    IFileResolverService fileResolverService)
    : IRequestHandler<ProcessBatchFromCsvCommand, SuccessResponse<string>>
{
    private readonly BatchSettings _batchSettings = AppSettingsAnn.AppSettings.BatchSettings;
    private readonly PrintboxSettings _printboxSettings = AppSettingsAnn.AppSettings.PrintboxSettings;

    public async Task<SuccessResponse<string>> Handle(ProcessBatchFromCsvCommand request, CancellationToken ct)
    {
        try
        {
            int processed = 0;
            int updated = 0;
            int skipped = 0;

            var tokenResponse = await printboxApi.GetTokenAsync(
                new Dictionary<string, string>
                {
                    ["client_id"] = _printboxSettings.ClientId,
                    ["client_secret"] = _printboxSettings.ClientSecret,
                    ["grant_type"] = "client_credentials"
                },
                ct);

            string bearer = $"Bearer {tokenResponse.AccessToken}";

            using var reader = new StreamReader(request.File.OpenReadStream());

            // ✅ LEER HEADER
            var headerLine = await reader.ReadLineAsync();

            if (headerLine == null)
                throw new Exception("CSV is empty.");

            var headers = headerLine.Split(',');

            int foIndex = Array.FindIndex(headers,
                h => h.Trim().Equals("FO Number", StringComparison.OrdinalIgnoreCase));
            int qtyIndex = Array.FindIndex(headers,
                h => h.Trim().Equals("Quantity", StringComparison.OrdinalIgnoreCase));

            if (foIndex == -1)
                throw new Exception("Column 'FO Number' not found in CSV.");

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var columns = line.Split(',');

                if (columns.Length <= foIndex)
                    continue;

                string rawFo = columns[foIndex].Trim();
                string fo = rawFo.Split('-')[0];

                if (fo.Length > 7)
                    fo = fo.Substring(0, 7);

                if (string.IsNullOrWhiteSpace(fo))
                    break;

                var quantity = int.Parse(columns[3]);

                var entity = await dbContext.Batches
                    .AsTracking()
                    .FirstOrDefaultAsync(
                        x => x.Fo == fo,
                        ct);

                // ✅ SI EXISTE ACTUALIZAR
                if (entity is not null)
                {
                    entity.Quantity = quantity;
                    entity.Status = "Batched";
                    entity.BatchDate = request.BatchDate;
                    entity.ModifiedAt = DateTime.UtcNow;
                    updated++;
                    continue;
                }

                // ✅ SI NO EXISTE CREAR
                var dto = await baanRepository.GetBatchDataAsync(
                    fo,
                    ct);

                entity = mapper.Map<BatchEntity>(dto);

                entity.Fo = fo;
                entity.Quantity = quantity;
                entity.Status = "Batched";
                entity.BatchDate = request.BatchDate;

                entity.Uuid = await ResolveUuidAsync(
                    entity.Magento,
                    bearer,
                    ct);

                var (files, date) = fileResolverService.ResolveFiles(fo, entity.Item);

                entity.Files = files;
                entity.FilesDate = date;

                await dbContext.Batches.AddAsync(
                    entity,
                    ct);

                processed++;
            }

            if (processed > 0 || updated > 0)
                await dbContext.SaveChangesAsync(ct);

            return new SuccessResponse<string>(
                "Success",
                $"Processed: {processed}, Updated: {updated}, Skipped: {skipped}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CSV batch processing failed");
            throw;
        }
    }

    #region PRIVATE

    private async Task<string> ResolveUuidAsync(
        string magento,
        string bearer,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(magento))
            return "";

        try
        {
            var order = await printboxApi.GetOrderAsync(
                $"b4com-{magento}",
                bearer,
                ct);

            return order?.Projects?.FirstOrDefault()?.Uuid ?? "";
        }
        catch
        {
            return "";
        }
    }   

    #endregion
}

#endregion