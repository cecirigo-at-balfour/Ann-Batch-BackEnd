using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Domain.Entities;
using Service.Ann.Batch.Api.Infrastructure.Persistence;

namespace Service.Ann.Batch.Api.Features.Settings;

#region 1. QUERY

public record GetAllSettingsQuery() : IRequest<List<Setting>>;

#endregion

#region 2. VALIDATOR

public class GetAllSettingsValidator : AbstractValidator<GetAllSettingsQuery>
{
    public GetAllSettingsValidator()
    {
        // No input fields → no rules needed
        // Se mantiene por consistencia arquitectónica
    }
}

#endregion

#region 3. HANDLER

public class GetAllSettingsHandler : IRequestHandler<GetAllSettingsQuery, List<Setting>>
{
    private readonly AppDbContext _context;

    public GetAllSettingsHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Setting>> Handle(GetAllSettingsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Settings
            .OrderBy(x => x.ConfigKey)
            .ToListAsync(cancellationToken);
    }
}

#endregion

#region 4. CONTROLLER

[ApiController]
[Route("settings")]
[Tags("Settings")]
public class GetAllSettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public GetAllSettingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets all system settings ordered by key
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Setting>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAllSettingsQuery(), ct);
        return Ok(result);
    }
}

#endregion