using BCrypt.Net;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Ann.Batch.Api.Domain.Dtos.Users;
using Service.Ann.Batch.Api.Infrastructure.Persistence;
using Service.Ann.Batch.Api.Infrastructure.Security;


namespace Service.Ann.Batch.Api.Features.Users;

#region COMMAND

public record LoginUserCommand(
    string Username,
    string Password
) : IRequest<LoginUserResponse>;

#endregion

#region RESPONSE

public class LoginUserResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

#endregion

#region VALIDATOR

public class LoginUserValidator
    : AbstractValidator<LoginUserCommand>
{
    public LoginUserValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}

#endregion

#region CONTROLLER

[ApiController]
[Route("auth")]
[AllowAnonymous]
public class LoginUserController(IMediator mediator)
    : ControllerBase
{
    /// <summary>
    /// User login
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginUserCommand command,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);

        return Ok(result);
    }
}

#endregion


#region HANDLER

public class LoginUserHandler(
    AppDbContext context,
    IJwtTokenGenerator jwtTokenGenerator)
    : IRequestHandler<LoginUserCommand, LoginUserResponse>
{
    public async Task<LoginUserResponse> Handle(
        LoginUserCommand request,
        CancellationToken ct)
    {
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Username == request.Username &&
                     !x.IsDeleted,
                ct);

        if (user == null)
        {
            return new LoginUserResponse
            {
                Success = false,
                Message = "Invalid username or password"
            };
        }

        var isValid = BCrypt.Net.BCrypt.Verify(
            request.Password,
            user.PasswordHash);

        if (!isValid)
        {
            return new LoginUserResponse
            {
                Success = false,
                Message = "Invalid username or password"
            };
        }

        var token = jwtTokenGenerator.GenerateToken(user);

        return new LoginUserResponse
        {
            Success = true,
            Message = "Login successful",
            Token = token,
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role
        };
    }
}

#endregion
