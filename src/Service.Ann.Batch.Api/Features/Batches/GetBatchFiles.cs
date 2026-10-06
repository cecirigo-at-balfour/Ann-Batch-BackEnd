using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Application.Wrappers;
using Service.Ann.Batch.Api.Common.Settings;
using Service.Ann.Batch.Api.Infrastructure.Persistence;
using Service.Ann.Batch.Api.Infrastructure.Services;
using System.Text.Json;

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
    ILogger<GetBatchFilesHandler> logger,
    IFileResolverService fileResolverService)
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

        var (files, date) = fileResolverService.ResolveFiles(entity.Fo, entity.Item);

        Console.WriteLine(files);
        entity.Files = files;
        entity.FilesDate = date;

        dbContext.Batches.Update(entity);
        var rows = await dbContext.SaveChangesAsync(ct);

        Console.WriteLine($"Rows affected: {rows}");

        logger.LogInformation(
            "Files updated for FO {Fo}",
            request.Fo);

        return new SuccessResponse<string>(
            "Success",
            "Files updated successfully");
    }
       
}

#endregion