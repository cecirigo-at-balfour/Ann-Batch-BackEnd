using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Service.Ann.Batch.Api.Application.Wrappers;
using Service.Ann.Batch.Api.Common.Settings;
using Service.Ann.Batch.Api.Domain.Dtos.Baan;
using Service.Ann.Batch.Api.Domain.Entities;
using Service.Ann.Batch.Api.Infrastructure.DataAccess.Baan;
using Service.Ann.Batch.Api.Infrastructure.Persistence;
using Service.Ann.Batch.Api.Infrastructure.Services;
using System.Text.RegularExpressions;

namespace Service.Ann.Batch.Api.Features.Batch;

#region 1. COMMAND

public record ProcessBatchCommand(
    IFormFile File,
    DateTime BatchDate
) : IRequest<SuccessResponse<string>>;

#endregion

#region 2. VALIDATOR

public sealed class ProcessBatchValidator : AbstractValidator<ProcessBatchCommand>
{
    public ProcessBatchValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("File is required.")
            .Must(f => f.Length > 0)
            .WithMessage("File is empty.")
            .Must(f => Path.GetExtension(f.FileName).ToLower() == ".txt")
            .WithMessage("Only .txt files are allowed.");

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
        CreateMap<BatchEntity, BatchResponse>();
    }
}

public class BatchResponse
{
    public string Fo { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string Item { get; set; } = "";
    public string Uuid { get; set; } = "";
}

#endregion

#region 4. CONTROLLER

[ApiController]
[Route("batch")]
[Tags("Batch")]
public class ProcessBatchController(IMediator mediator) : ControllerBase
{    
    [HttpPost("process")]
    [ProducesResponseType(typeof(SuccessResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Post(IFormFile file,[FromForm] DateTime batchDate,CancellationToken ct)
    {
        var response = await mediator.Send(
            new ProcessBatchCommand(file, batchDate), ct);

        return Ok(response);
    }
}

#endregion

#region 5. HANDLER

public sealed class ProcessBatchHandler(AppDbContext dbContext,IBaanRepository baanRepository,IPrintboxApi printboxApi,IMapper mapper,ILogger<ProcessBatchHandler> logger, IFileResolverService fileResolverService) :IRequestHandler<ProcessBatchCommand, SuccessResponse<string>>
{
    private readonly BatchSettings _batchSettings = AppSettingsAnn.AppSettings.BatchSettings;
    private readonly PrintboxSettings _printboxSettings = AppSettingsAnn.AppSettings.PrintboxSettings;

    public async Task<SuccessResponse<string>> Handle(ProcessBatchCommand request,CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Starting Batch Process...");

            int processed = 0;
            var tokenResponse = await printboxApi.GetTokenAsync(
                new Dictionary<string, string>
                {
                    ["client_id"] = _printboxSettings.ClientId,
                    ["client_secret"] = _printboxSettings.ClientSecret,
                    ["grant_type"] = "client_credentials"
                },
                ct);

            string bearer = $"Bearer {tokenResponse.AccessToken}";

            using var stream = request.File.OpenReadStream();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var fields = Regex.Split(line.Trim(), @"\s{2,}");
                if (fields.Length < 4)
                    continue;

                string fo = line.Substring(0, 8).Trim();
                string school = fields.ElementAtOrDefault(1) ?? "";
                string student = fields.ElementAtOrDefault(3) ?? "";
                string item = fields.ElementAtOrDefault(5) ?? "";

                var dto = await baanRepository.GetBatchDataAsync(fo, ct);

                var entity = mapper.Map<BatchEntity>(dto);

                entity.Fo = fo;
                entity.School = school;
                entity.StudentName = student;
                entity.Item = item;
                entity.BatchDate = request.BatchDate;
                entity.BookDate = request.BatchDate;
                entity.Status = "Backordered";

                entity.Uuid = await ResolveUuidAsync(
                    entity.Magento,
                    bearer,
                    ct);

                var (files, date) = fileResolverService.ResolveFiles(fo, item);
                entity.Files = files;
                entity.FilesDate = date;

                await dbContext.Batches.AddAsync(entity, ct);
                           
                processed++;
            }

            if (processed > 0)
                await dbContext.SaveChangesAsync(ct);

            logger.LogInformation("Batch completed: {Count}", processed);

            return new SuccessResponse<string>(
                "Success",
                $"{processed} records processed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Batch processing failed");
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