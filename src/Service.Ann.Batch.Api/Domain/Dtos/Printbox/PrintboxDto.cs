using System.Text.Json.Serialization;

namespace Service.Ann.Batch.Api.Domain.Dtos.Printbox;

public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";
}

public class OrderResponse
{
    public List<ProjectItem>? Projects { get; set; }
}

public class ProjectItem
{
    public string? Uuid { get; set; }
}
