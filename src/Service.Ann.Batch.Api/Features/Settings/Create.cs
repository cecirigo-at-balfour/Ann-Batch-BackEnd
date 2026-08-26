using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Service.Ann.Batch.Api.Infrastructure.Persistence;

namespace Service.Ann.Batch.Api.Features.Settings;

#region 1. COMMAND

public record CreateSettingCommand(string ConfigKey, string ConfigValue) : IRequest<Guid>;

#endregion

#region 2. VALIDATOR

public class CreateSettingValidator : AbstractValidator<CreateSettingCommand>
{
    public CreateSettingValidator()
    {
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

#region 3. HANDLER

public class CreateSettingHandler : IRequestHandler<CreateSettingCommand, Guid>
{
    private readonly AppDbContext _context;

    public CreateSettingHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateSettingCommand request, CancellationToken cancellationToken)
    {
        var entity = new Setting
        {
            ConfigKey = request.ConfigKey,
            ConfigValue = request.ConfigValue
        };

        _context.Settings.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}

#endregion

#region 4. CONTROLLER

[ApiController]
[Route("settings")]
[Tags("Settings")]
public class CreateSettingController : ControllerBase
{
    private readonly IMediator _mediator;

    public CreateSettingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new setting
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] CreateSettingCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }
}

#endregion