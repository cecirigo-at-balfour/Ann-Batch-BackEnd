
namespace Service.Ann.Batch.Api.Domain.Dtos.Users;

public class UserDto
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
    
}