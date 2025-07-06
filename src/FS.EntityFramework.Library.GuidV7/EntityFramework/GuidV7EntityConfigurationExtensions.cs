using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FS.EntityFramework.Library.GuidV7.EntityFramework;

/// <summary>
/// Database-agnostic Entity Framework configuration for GUID V7
/// </summary>
public static class GuidV7EntityConfigurationExtensions
{
    /// <summary>
    /// Configures GUID V7 property for optimal sequential performance
    /// </summary>
    public static PropertyBuilder<Guid> ConfigureAsGuidV7<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, Guid>> propertyExpression) where TEntity : class
    {
        return builder.Property(propertyExpression)
            .ValueGeneratedNever();                         // ✅ Application generates, not database
    }

    /// <summary>
    /// Configures nullable GUID V7 property
    /// </summary>
    public static PropertyBuilder<Guid?> ConfigureAsGuidV7<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, Guid?>> propertyExpression) where TEntity : class
    {
        return builder.Property(propertyExpression)
            .ValueGeneratedNever();
    }
}