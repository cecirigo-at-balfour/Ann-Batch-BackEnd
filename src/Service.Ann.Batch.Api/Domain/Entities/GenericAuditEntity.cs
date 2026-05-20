namespace Service.Ann.Batch.Api.Domain.Entities;

public class GenericAuditEntity : GenericEntity
{
    public DateTime? ModifiedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}
