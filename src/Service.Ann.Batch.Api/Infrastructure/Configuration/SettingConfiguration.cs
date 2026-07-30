namespace Service.Ann.Batch.Api.Infrastructure.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Ann.Batch.Api.Domain.Entities;

public class SettingConfiguration
{
    public void Configure(EntityTypeBuilder<SettingEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ConfigKey)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.ConfigValue)
            .IsRequired();

        builder.HasIndex(x => x.ConfigKey)
            .IsUnique();

        // Soft delete global filter (muy recomendado)
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}