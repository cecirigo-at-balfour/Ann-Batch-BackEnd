using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Service.Ann.Batch.Api.Domain.Dtos.Users;
using Service.Ann.Batch.Api.Domain.Entities;
using Service.Ann.Batch.Api.Infrastructure.Persistence;
using System.Data;

namespace Service.Ann.Batch.Api.Features.Users;

#region COMMAND

public record CreateUserCommand(
    string Username,
    string Password,
    string FullName,
    string Role
) : IRequest<Domain.Dtos.Users.UserDto>;

#endregion


#region VALIDATOR

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty();
        RuleFor(x => x.Role).NotEmpty();
    }
}

#endregion

#region CONTROLLER

[ApiController]
[Route("users")]
[Tags("Users")]
public class CreateUserController(IMediator mediator)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserCommand command,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);

        return Ok(result);
    }
}

#endregion

#region HANDLER


public class CreateUserHandler(AppDbContext dbContext) : IRequestHandler<CreateUserCommand, UserDto>

{
       public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var entity = new UserEntity
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Role = request.Role,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        dbContext.Users.Add(entity);

        await dbContext.SaveChangesAsync(ct);


        return new UserDto
        {
            Id = entity.Id,
            Username = entity.Username,
            FullName = entity.FullName,
            Role = entity.Role
        };

    }
}

#endregion