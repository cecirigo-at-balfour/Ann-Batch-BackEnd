using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Service.Ann.Batch.Api.Domain.Entities;
using Service.Ann.Batch.Api.Infrastructure.Configuration;
using System.Text.RegularExpressions;

namespace Service.Ann.Batch.Api.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ILoggerFactory _loggerFactory;

    public AppDbContext(DbContextOptions<AppDbContext> options, ILoggerFactory loggerFactory) : base(options)
    {
       // ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking; se comento para que pueda actualizar la informacion
        _loggerFactory = loggerFactory;
    }

    public DbSet<BatchEntity> Batches => Set<BatchEntity>();
    public DbSet<SettingEntity> Settings => Set<SettingEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();


    /// <summary>
    /// Automatically intercept tracks to intercept GenericAuditEntity changes before committing to MySQL.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<GenericAuditEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAt = now;
            }
            else if (entry.State == EntityState.Deleted)
            {
                // Soft-delete interceptor
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = now;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Explicitly apply individual mapping criteria overrides
        new BatchConfiguration().Configure(builder.Entity<BatchEntity>());
        new SettingConfiguration().Configure(builder.Entity<SettingEntity>());

        base.OnModelCreating(builder);

        // Dynamically overwrite Table and Column naming schemas into standard snake_case styles
        AdjustNamesForMySqlStandard(builder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseLoggerFactory(_loggerFactory);
      
    }

    /// <summary>
    /// Standardizes PascalCase object definitions to snake_case database metadata properties (e.g., email_logs).
    /// </summary>
    /// <param name="modelBuilder">Table model builder reference.</param>
    private static void AdjustNamesForMySqlStandard(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            modelBuilder.Entity(entityType.ClrType).ToTable(entityType.ClrType.Name.ToSnakeCase());
            foreach (var property in entityType.GetProperties())
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(property.Name)
                    .HasColumnName(property.Name.ToSnakeCase());
            }
        }
    }
}

static partial class NameConventions
{
    [GeneratedRegex("^_")]
    private static partial Regex UnderscoreRegex();

    [GeneratedRegex("(?:(?<l>[a-z0-9])(?<r>[A-Z])|(?<l>[A-Z])(?<r>[A-Z][a-z0-9]))")]
    private static partial Regex AlphanumericRegex();

    /// <summary>
    /// Convert a string metadata tag to snake_case patterns.
    /// </summary>
    /// <param name="input">Original text context.</param>
    /// <returns>Text transformed into clean snake_case outputs.</returns>
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var noLeadingUnderscore = UnderscoreRegex().Replace(input.Trim(), "");
        return AlphanumericRegex().Replace(noLeadingUnderscore, "${l}_${r}").ToLowerInvariant();
    }
}