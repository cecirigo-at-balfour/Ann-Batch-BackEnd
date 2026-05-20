namespace Service.Ann.Batch.Api.Application.Wrappers;

public class SuccessResponse<T>
{
    public SuccessResponse() { }
    public SuccessResponse(T data, string? message = null)
    {
        Succeeded = true;
        Message = message;
        Data = data;
    }

    public SuccessResponse(string message)
    {
        Succeeded = false;
        Message = message;
    }

    /// <summary>
    /// Succeeded response
    /// </summary>
    /// <example>true</example>
    public bool Succeeded { get; set; }
    /// <summary>
    ///  Message response
    /// </summary>
    /// <example>successful</example>
    public string? Message { get; set; }
    /// <summary>
    /// Data response
    /// </summary>
    public T? Data { get; set; }
}
