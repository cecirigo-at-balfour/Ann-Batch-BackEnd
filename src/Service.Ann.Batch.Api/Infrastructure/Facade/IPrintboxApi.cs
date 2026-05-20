using Refit;
using Service.Ann.Batch.Api.Domain.Dtos.Printbox;

public interface IPrintboxApi
{
    [Post("/o/token/")]
    Task<TokenResponse> GetTokenAsync(
        [Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, string> body,
        CancellationToken ct = default);

    [Get("/api/ec/v4/orders/{orderNumber}/")]
    Task<OrderResponse> GetOrderAsync(
        string orderNumber,
        [Header("Authorization")] string authorization,
        CancellationToken ct = default);
}