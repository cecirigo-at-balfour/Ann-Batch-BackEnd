using BCrypt.Net;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Infrastructure.Persistence;

namespace Service.Ann.Batch.Api.Features.Users;

#region COMMAND

public record ChangePasswordCommand(
    int UserId,
    string CurrentPassword,
    string NewPassword
) : IRequest<bool>;

#endregion

#region VALIDATOR

public class ChangePasswordValidator
    : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0);

        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("Current password is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required")
            .MinimumLength(8)
            .WithMessage("New password must be at least 8 characters");

        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("New password must be different from current password");
    }
}

#endregion

#region CONTROLLER

[ApiController]
[Route("users")]
[Tags("Users")]
public class ChangePasswordController(IMediator mediator)
    : ControllerBase
{
    /// <summary>
    /// Change user password
    /// </summary>
    [HttpPut("{userId}/change-password")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(
        int userId,
        [FromBody] ChangePasswordCommand command,
        CancellationToken ct)
    {
        var request = command with { UserId = userId };

        var result = await mediator.Send(request, ct);

        return Ok(result);
    }
}

#endregion

#region HANDLER

public class ChangePasswordHandler(
    AppDbContext context)
    : IRequestHandler<ChangePasswordCommand, bool>
{
    public async Task<bool> Handle(
        ChangePasswordCommand request,
        CancellationToken ct)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(
                x => x.Id == request.UserId &&
                     !x.IsDeleted,
                ct);

        if (user == null)
            throw new Exception("User not found");

        var isCurrentPasswordValid =
            BCrypt.Net.BCrypt.Verify(
                request.CurrentPassword,
                user.PasswordHash);

        if (!isCurrentPasswordValid)
            throw new Exception("Current password is invalid");

        user.PasswordHash =
            BCrypt.Net.BCrypt.HashPassword(
                request.NewPassword);

        user.ModifiedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        return true;
    }
}

#endregion