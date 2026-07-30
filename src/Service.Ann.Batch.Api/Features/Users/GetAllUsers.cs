using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Domain.Dtos.Users;
using Service.Ann.Batch.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;

namespace Service.Ann.Batch.Api.Features.Users;

#region QUERY

public record GetUsersQuery()
    : IRequest<List<UserDto>>;

#endregion

#region CONTROLLER

[ApiController]
[Route("users")]
[Tags("Users")]
[Authorize]
public class GetUsersController(IMediator mediator)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        CancellationToken ct)
    {
        return Ok(
            await mediator.Send(
                new GetUsersQuery(),
                ct));
    }
}

#endregion

#region HANDLER

public class GetUsersHandler(
    AppDbContext dbContext)
    : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    public async Task<List<UserDto>> Handle(
        GetUsersQuery request,
        CancellationToken ct)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Select(x => new UserDto{
                Id = x.Id,
                Username = x.Username,
                FullName = x.FullName,
                Role = x.Role
            })
            .ToListAsync(ct);
    }
}

#endregion