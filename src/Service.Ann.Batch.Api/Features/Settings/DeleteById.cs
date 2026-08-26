using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Infrastructure.Persistence;

namespace Service.Ann.Batch.Api.Features.Settings;

#region 1. COMMAND

/// <summary>
/// Command to delete a setting by Id
/// </summary>
/// <param name="Id" example="1">
/// Setting Id to delete
/// </param>
public record DeleteSettingByIdCommand(Guid Id) : IRequest<bool>;

#endregion

#region 2. VALIDATOR

public class DeleteSettingByIdValidator : AbstractValidator<DeleteSettingByIdCommand>
{
    public DeleteSettingByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty)
            .WithMessage("Id must be a valid GUID.");
    }
}

#endregion

#region 3. CONTROLLER

[ApiController]
[Route("settings")]
[Tags("Settings")]
public class DeleteSettingByIdController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Deletes a setting by Id
    /// </summary>
    /// <param name="id" example="1">
    /// Setting Id
    /// </param>
    [HttpDelete("by-id/{id}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteSettingByIdCommand(id), ct);

        if (!result)
            return NotFound("Setting not found");

        return Ok(result);
    }
}

#endregion

#region 4. HANDLER

public class DeleteSettingByIdHandler : IRequestHandler<DeleteSettingByIdCommand, bool>
{
    private readonly AppDbContext _context;

    public DeleteSettingByIdHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteSettingByIdCommand request, CancellationToken ct)
    {
        var entity = await _context.Settings
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (entity == null)
            return false;

        _context.Settings.Remove(entity);

        await _context.SaveChangesAsync(ct);

        return true;
    }
}

#endregion