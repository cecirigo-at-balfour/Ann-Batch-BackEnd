namespace Service.Ann.Batch.Api.Infrastructure.Configuration;

using global::Service.Ann.Batch.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AnnfileConfiguration
    : IEntityTypeConfiguration<Annfile>
{
    public void Configure(EntityTypeBuilder<Annfile> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasMaxLength(100);

        builder.Property(x => x.PathToFile)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.BatchId)
            .IsRequired();

        builder.Property(x => x.Fo)
            .HasMaxLength(10)
            .IsRequired();

     /*   builder.HasOne(x => x.Batch)
            .WithMany()
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade); */
    }
}