namespace Service.Ann.Batch.Api.Infrastructure.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SettingConfiguration
{
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.ToTable("settings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.ConfigKey).IsRequired().HasMaxLength(150);

        builder.Property(x => x.ConfigValue).IsRequired();

        builder.HasIndex(x => x.ConfigKey).IsUnique();

        // Soft delete global filter (muy recomendado)
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}