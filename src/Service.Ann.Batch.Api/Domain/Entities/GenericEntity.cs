namespace Service.Ann.Batch.Api.Domain.Entities;

public class GenericEntity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }

    protected GenericEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }
}
