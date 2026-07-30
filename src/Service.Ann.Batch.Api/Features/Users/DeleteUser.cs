using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Infrastructure.Persistence;

namespace Service.Ann.Batch.Api.Features.Users;

#region COMMAND

public record DeleteUserCommand(int Id)
    : IRequest<bool>;

#endregion

#region CONTROLLER

[ApiController]
[Route("users")]
[Tags("Users")]
public class DeleteUserController(IMediator mediator)
    : ControllerBase
{
    /// <summary>
    /// Soft delete user
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new DeleteUserCommand(id),
            ct);

        return Ok(result);
    }
}

#endregion

#region HANDLER

public class DeleteUserHandler(
    AppDbContext context)
    : IRequestHandler<DeleteUserCommand, bool>
{
    public async Task<bool> Handle(
        DeleteUserCommand request,
        CancellationToken ct)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(
                x => x.Id == request.Id && !x.IsDeleted,
                ct);

        if (user == null)
            throw new Exception("User not found");

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.ModifiedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        return true;
    }
}

#endregion