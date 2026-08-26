using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Domain.Entities;
using Service.Ann.Batch.Api.Infrastructure.Persistence;

namespace Service.Ann.Batch.Api.Features.Settings;

#region 1. COMMAND

/// <summary>
/// Command to delete a setting by ConfigKey
/// </summary>
/// <param name="ConfigKey" example="ApiUrl">
/// Setting key to delete
/// </param>
public record DeleteSettingCommand(string ConfigKey) : IRequest<bool>;

#endregion

#region 2. VALIDATOR

public class DeleteSettingValidator : AbstractValidator<DeleteSettingCommand>
{
    public DeleteSettingValidator()
    {
        RuleFor(x => x.ConfigKey)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("ConfigKey is required and must be <= 100 chars");
    }
}

#endregion

#region 3. AUTOMAPPER (OPCIONAL)

public sealed class DeleteSettingMappingProfile : Profile
{
    public DeleteSettingMappingProfile()
    {
        CreateMap<DeleteSettingCommand, Setting>();
    }
}

#endregion

#region 4. CONTROLLER

[ApiController]
[Route("settings")]
[Tags("Settings")]
public class DeleteSettingController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Deletes a setting by ConfigKey
    /// </summary>
    /// <param name="configKey" example="ApiUrl">
    /// Configuration key to delete
    /// </param>
    [HttpDelete("{configKey}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] string configKey,
        CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteSettingCommand(configKey), ct);

        if (!result)
            return NotFound("Setting not found");

        return Ok(result);
    }
}

#endregion

#region 5. HANDLER

public class DeleteSettingHandler : IRequestHandler<DeleteSettingCommand, bool>
{
    private readonly AppDbContext _context;

    public DeleteSettingHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteSettingCommand request, CancellationToken ct)
    {
        var entity = await _context.Settings
            .FirstOrDefaultAsync(x => x.ConfigKey == request.ConfigKey, ct);

        if (entity == null)
            return false;

        _context.Settings.Remove(entity);

        await _context.SaveChangesAsync(ct);

        return true;
    }
}

#endregion
