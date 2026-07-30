using Service.Ann.Batch.Api.Domain.Entities;

namespace Service.Ann.Batch.Api.Application.Abstractions.AS400;

public interface IAnnouncementRepository
{
    Task<IReadOnlyList<Announcement>> GetAnnouncementsAsync(decimal startAs400, decimal endAs400, string status, CancellationToken ct = default);
    Task<Announcement?> GetByOrderAsync(long foOrder, CancellationToken ct = default);
    Task<IReadOnlyList<ShippingAddressEntity>> GetShippingDetailAsync(long orderId, CancellationToken ct = default);
}
