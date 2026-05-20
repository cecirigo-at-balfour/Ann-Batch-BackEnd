using Newtonsoft.Json;

namespace Service.Ann.Batch.Api.Application.Wrappers;

public class ErrorResponse
{
    /// <summary>
    /// Succeeded response
    /// </summary>
    /// <example>false</example>
    [JsonProperty("succeeded")]
    public bool Succeeded { get; set; }
    /// <summary>
    ///  Message response
    /// </summary>
    /// <example>One or more validation errors have occurred.</example>
    [JsonProperty("message")]
    public string? Message { get; set; }
    /// <summary>
    /// Error list response
    /// </summary>
    /// <example>["The attrubute field is required."]</example>
    [JsonProperty("errors")]
    public List<string>? Errors { get; set; }
}