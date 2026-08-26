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
/// Command to update an existing setting
/// </summary>
/// <param name="Id" example="1">Setting Id</param>
/// <param name="ConfigKey" example="ApiUrl">Setting key</param>
/// <param name="ConfigValue" example="https://api.test.com">Setting value</param>
public record UpdateSettingCommand(
    Guid Id,
    string ConfigKey,
    string ConfigValue
) : IRequest<bool>;

#endregion

#region 2. VALIDATOR

public class UpdateSettingValidator : AbstractValidator<UpdateSettingCommand>
{
    public UpdateSettingValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id is required");

        RuleFor(x => x.ConfigKey)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("ConfigKey is required and must be <= 100 chars");

        RuleFor(x => x.ConfigValue)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("ConfigValue is required and must be <= 500 chars");
    }
}

#endregion

#region 3. AUTOMAPPER

public sealed class UpdateSettingMappingProfile : Profile
{
    public UpdateSettingMappingProfile()
    {
        CreateMap<UpdateSettingCommand, Setting>()
            .ForMember(dest => dest.Id, opt => opt.Ignore()); // Id no se mapea automáticamente
    }
}

#endregion

#region 4. CONTROLLER

[ApiController]
[Route("settings")]
[Tags("Settings")]
public class UpdateSettingController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Updates an existing setting
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateSettingCommand command,
        CancellationToken ct)
    {
        // ✅ asegurar consistencia
        command = command with { Id = id };

        var result = await mediator.Send(command, ct);

        if (!result)
            return NotFound("Setting not found");

        return Ok(result);
    }
}

#endregion

#region 5. HANDLER

public class UpdateSettingHandler : IRequestHandler<UpdateSettingCommand, bool>
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public UpdateSettingHandler(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<bool> Handle(UpdateSettingCommand request, CancellationToken ct)
    {
        var entity = await _context.Settings
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (entity == null)
            return false;

        // ✅ Mapping (actualiza valores)
        _mapper.Map(request, entity);

        await _context.SaveChangesAsync(ct);

        return true;
    }
}

#endregion
