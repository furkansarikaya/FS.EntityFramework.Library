using System.Linq.Expressions;
using FS.EntityFramework.Library.UlidGenerator.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FS.EntityFramework.Library.UlidGenerator.EntityFramework;

/// <summary>
/// Simple, database-agnostic Entity Framework configuration for ULID
/// </summary>
public static class UlidEntityConfigurationExtensions
{
    /// <summary>
    /// Configures ULID property with basic database storage as string
    /// </summary>
    public static PropertyBuilder<Ulid> ConfigureAsUlid<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, Ulid>> propertyExpression) where TEntity : class
    {
        return builder.Property(propertyExpression)
            .HasConversion<string>()                        // ✅ Simple conversion to string
            .HasMaxLength(26);                              // ✅ ULID length
    }

    /// <summary>
    /// Configures nullable ULID property
    /// </summary>
    public static PropertyBuilder<Ulid?> ConfigureAsUlid<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, Ulid?>> propertyExpression) where TEntity : class
    {
        return builder.Property(propertyExpression)
            .HasConversion<string>()
            .HasMaxLength(26);
    }

    /// <summary>
    /// Applies ULID configuration to all entities with ULID keys (database-agnostic)
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    public static void ConfigureUlidEntities(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var primaryKey = entityType.FindPrimaryKey();
            if (primaryKey?.Properties.Count != 1) continue;
            var keyProperty = primaryKey.Properties[0];
            if (keyProperty.ClrType != typeof(Ulid) &&
                keyProperty.ClrType != typeof(Ulid?)) continue;
            // ✅ Simple configuration without database-specific features
            var entityBuilder = modelBuilder.Entity(entityType.ClrType);
                    
            entityBuilder.Property(keyProperty.Name)
                .HasConversion(
                    typeof(string),                     // Store as string
                    typeof(UlidConverter))              // Custom converter
                .HasMaxLength(26)
                .IsRequired();

            entityBuilder.HasIndex(keyProperty.Name);
        }
    }
}