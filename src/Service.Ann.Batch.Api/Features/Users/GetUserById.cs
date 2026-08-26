using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Domain.Dtos.Users;
using Service.Ann.Batch.Api.Infrastructure.Persistence;

namespace Service.Ann.Batch.Api.Features.Users;

#region QUERY

public record GetUserByIdQuery(Guid Id)
    : IRequest<UserDto>;

#endregion

#region CONTROLLER

[ApiController]
[Route("users")]
[Tags("Users")]
public class GetUserByIdController(IMediator mediator)
    : ControllerBase
{
    /// <summary>
    /// Get user by Id
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        Guid id,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetUserByIdQuery(id),
            ct);

        return Ok(result);
    }
}

#endregion

#region HANDLER

public class GetUserByIdHandler(
    AppDbContext context)
    : IRequestHandler<GetUserByIdQuery, UserDto>
{
    public async Task<UserDto> Handle(
        GetUserByIdQuery request,
        CancellationToken ct)
    {
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == request.Id && !x.IsDeleted,
                ct);

        if (user is null)
            throw new Exception("User not found");

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role          
        };
    }
}

#endregion
