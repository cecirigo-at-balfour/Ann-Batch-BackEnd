using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Infrastructure.Persistence;

namespace Service.Ann.Batch.Api.Features.Settings;

/// <summary>
/// Query to get config value by key
/// </summary>
/// <param name="ConfigKey">Configuration key</param>
public record GetSettingQuery(string ConfigKey) : IRequest<string?>;

public class GetSettingValidator : AbstractValidator<GetSettingQuery>
{
    public GetSettingValidator()
    {
        RuleFor(x => x.ConfigKey)
            .NotEmpty()
            .WithMessage("ConfigKey is required");
    }
}

[ApiController]
[Route("settings")]
[Tags("Settings")]
public class GetSettingController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Get config value by key
    /// </summary>
    [HttpGet("{configKey}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string configKey, CancellationToken ct)
    {
        var result = await mediator.Send(new GetSettingQuery(configKey), ct);

        if (result == null)
            return NotFound($"ConfigKey '{configKey}' not found");

        return Ok(result);
    }
}

public class GetSettingHandler : IRequestHandler<GetSettingQuery, string?>
{
    private readonly AppDbContext _context;
    private readonly ILogger<GetSettingHandler> _logger;

    public GetSettingHandler(AppDbContext context, ILogger<GetSettingHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string?> Handle(GetSettingQuery request, CancellationToken ct)
    {
        try
        {
            var setting = await _context.Set<Setting>()
               .AsNoTracking()
               .FirstOrDefaultAsync(x => x.ConfigKey == request.ConfigKey, ct);

            if (setting == null)
            {
                _logger.LogWarning("ConfigKey {ConfigKey} not found", request.ConfigKey);
                return null;
            }

            _logger.LogInformation("ConfigKey {ConfigKey} retrieved", request.ConfigKey);

            return setting.ConfigValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving setting");
            throw;
        }
    }
}

