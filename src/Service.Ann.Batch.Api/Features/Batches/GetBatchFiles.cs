using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Application.Wrappers;
using Service.Ann.Batch.Api.Common.Settings;
using Service.Ann.Batch.Api.Infrastructure.Persistence;

namespace Service.Ann.Batch.Api.Features.Batch;

#region COMMAND

public record GetBatchFilesCommand(string Fo)
    : IRequest<SuccessResponse<string>>;

#endregion

#region VALIDATOR

public sealed class GetBatchFilesValidator
    : AbstractValidator<GetBatchFilesCommand>
{
    public GetBatchFilesValidator()
    {
        RuleFor(x => x.Fo)
            .NotEmpty();
    }
}

#endregion

#region CONTROLLER

[ApiController]
[Route("batch")]
[Tags("Batch")]
public class GetBatchFilesController(IMediator mediator)
    : ControllerBase
{
    [HttpPost("get-files")]
    public async Task<IActionResult> Post(
        [FromBody] GetBatchFilesCommand command,
        CancellationToken ct)
    {
        var response = await mediator.Send(command, ct);

        return Ok(response);
    }
}

#endregion

#region HANDLER

public sealed class GetBatchFilesHandler(
    AppDbContext dbContext,
    ILogger<GetBatchFilesHandler> logger)
    : IRequestHandler<GetBatchFilesCommand, SuccessResponse<string>>
{
    private readonly BatchSettings _batchSettings =
        AppSettingsAnn.AppSettings.BatchSettings;

    public async Task<SuccessResponse<string>> Handle(
        GetBatchFilesCommand request,
        CancellationToken ct)
    {
        var entity = await dbContext.Batches
            .FirstOrDefaultAsync(x => x.Fo == request.Fo, ct);

        if (entity is null)
            throw new Exception("FO not found");

        var (files, date) = ResolveFiles(entity.Fo, entity.Item);

        entity.Files = files;
        entity.FilesDate = date;

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "Files updated for FO {Fo}",
            request.Fo);

        return new SuccessResponse<string>(
            "Success",
            "Files updated successfully");
    }

    private (string files, DateTime?) ResolveFiles(
        string fo,
        string item)
    {
        string dir = _batchSettings.DefaultDirectory;

        if (item.Equals(
            "GRETADLAB DGTL",
            StringComparison.OrdinalIgnoreCase))
        {
            dir = _batchSettings.AdLabDirectory;
        }

        if (item.Contains(
            "PERSONAL",
            StringComparison.OrdinalIgnoreCase))
        {
            dir = _batchSettings.PersonalNoteDirectory;
        }

        var matches = Directory.EnumerateFiles(dir)
            .Where(f => Path.GetFileName(f).Contains(fo))
            .Select(f => new FileInfo(f))
            .OrderBy(f => f.CreationTime)
            .ToList();

        return (
            string.Join("; ", matches.Select(f => f.Name)),
            matches.FirstOrDefault()?.CreationTime
        );
    }
}

#endregion