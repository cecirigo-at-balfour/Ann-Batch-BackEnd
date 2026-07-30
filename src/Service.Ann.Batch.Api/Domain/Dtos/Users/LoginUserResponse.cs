namespace Service.Ann.Batch.Api.Domain.Dtos.Users;

public class LoginUserResponse
{
    public string Token { get; set; } = string.Empty;

    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}