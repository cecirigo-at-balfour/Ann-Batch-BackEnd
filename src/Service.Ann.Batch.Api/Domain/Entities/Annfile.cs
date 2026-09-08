namespace Service.Ann.Batch.Api.Domain.Entities;

public class Annfile : GenericAuditEntity
{
    public string PathToFile { get; set; } = string.Empty;

    public Guid BatchId { get; set; } 

    public string Fo { get; set; } = string.Empty;

    // Navigation Property
 //   public BatchEntity Batch { get; set; } = null!;

}
