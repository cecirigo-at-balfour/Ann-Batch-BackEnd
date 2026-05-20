using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Service.Ann.Batch.Api.Domain.Entities;

namespace Service.Ann.Batch.Api.Infrastructure.Configuration;

public class BatchConfiguration : IEntityTypeConfiguration<BatchEntity>
{
    public void Configure(EntityTypeBuilder<BatchEntity> builder)
    {
        builder.ToTable("batches");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.BatchDate).IsRequired();
        builder.Property(x => x.Fo).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Item).HasMaxLength(255);
        builder.Property(x => x.School).HasMaxLength(255);
        builder.Property(x => x.StudentName).HasMaxLength(255);
        builder.Property(x => x.StudentLastName).HasMaxLength(255);
        builder.Property(x => x.Sr).HasMaxLength(50);
        builder.Property(x => x.Magento).HasMaxLength(50);
        builder.Property(x => x.Po).HasMaxLength(50);
        builder.Property(x => x.Uuid).HasMaxLength(255);
        builder.Property(x => x.ShipTracking).HasMaxLength(255);
        builder.Property(x => x.ShipMethod).HasMaxLength(100);
        builder.Property(x => x.ShipAddress).HasMaxLength(255);
        builder.Property(x => x.ShipCity).HasMaxLength(255);
        builder.Property(x => x.ShipState).HasMaxLength(255);
        builder.Property(x => x.ShipCode).HasMaxLength(50);        
        builder.Property(x => x.Files).HasMaxLength(1255);
        builder.Property(x => x.FilesDate);      
        builder.Property(x => x.Priority).HasMaxLength(50);
        builder.Property(x => x.Status).HasMaxLength(100);
    }
}