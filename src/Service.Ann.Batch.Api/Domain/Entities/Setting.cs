using Service.Ann.Batch.Api.Domain.Entities;

public class Setting : GenericAuditEntity
{
    public required string ConfigKey { get; set; }
    public required string ConfigValue { get; set; }
}
