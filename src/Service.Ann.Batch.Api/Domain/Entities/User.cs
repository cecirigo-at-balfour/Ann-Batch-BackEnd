namespace Service.Ann.Batch.Api.Domain.Entities;

public class User : GenericAuditEntity
{   
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    
}