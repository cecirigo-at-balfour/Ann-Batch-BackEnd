using Service.Ann.Batch.Api.Domain.Entities;

public class SettingEntity : GenericAuditEntity
{
    public int Id { get; set; }

    public string ConfigKey { get; set; } = null!;

    public string ConfigValue { get; set; } = null!;
}
