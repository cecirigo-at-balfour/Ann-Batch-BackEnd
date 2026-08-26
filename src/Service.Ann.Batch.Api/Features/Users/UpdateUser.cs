using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Infrastructure.Persistence;

namespace Service.Ann.Batch.Api.Features.Users;

#region COMMAND

public record UpdateUserCommand(
    Guid Id,
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
            .NotEqual(Guid.Empty);

        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Role)
            .NotEmpty()
            .MaximumLength(50);

        When(x => !string.IsNullOrWhiteSpace(x.Password), () =>
        {
            RuleFor(x => x.Password)
                .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters.");
        });
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
        Guid id,
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
            .AsTracking()
            .FirstOrDefaultAsync(
                x => x.Id == request.Id && !x.IsDeleted,
                ct);

        if (user == null)
            throw new Exception("User not found");

        user.Username = request.Username;
        user.FullName = request.FullName;
        user.Role = request.Role;
        user.ModifiedAt = DateTime.UtcNow;

        // Solo actualizar password si fue enviado
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        await context.SaveChangesAsync(ct);

        return true;
    }
}

#endregion