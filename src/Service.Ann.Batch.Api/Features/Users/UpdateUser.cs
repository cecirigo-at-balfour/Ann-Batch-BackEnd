using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Infrastructure.Persistence;

namespace Service.Ann.Batch.Api.Features.Users;

#region COMMAND

public record UpdateUserCommand(
    int Id,
    string Username,
    string FullName,
    string Password,
    string Role
) : IRequest<bool>;

#endregion

#region VALIDATOR

public class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Password).NotEmpty();

        RuleFor(x => x.Role)
            .NotEmpty()
            .MaximumLength(50);
    }
}

#endregion

#region CONTROLLER

[ApiController]
[Route("users")]
[Tags("Users")]
public class UpdateUserController(IMediator mediator)
    : ControllerBase
{
    /// <summary>
    /// Update user
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateUserCommand command,
        CancellationToken ct)
    {
        var request = command with { Id = id };

        var result = await mediator.Send(request, ct);

        return Ok(result);
    }
}

#endregion

#region HANDLER

public class UpdateUserHandler(
    AppDbContext context)
    : IRequestHandler<UpdateUserCommand, bool>
{
    public async Task<bool> Handle(
        UpdateUserCommand request,
        CancellationToken ct)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(
                x => x.Id == request.Id && !x.IsDeleted,
                ct);

        if (user == null)
            throw new Exception("User not found");

        user.Username = request.Username;
        user.FullName = request.FullName;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        user.Role = request.Role;
        user.ModifiedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        return true;
    }
}

#endregion