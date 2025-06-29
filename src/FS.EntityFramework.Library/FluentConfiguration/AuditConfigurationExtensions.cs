namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Fluent configuration extensions for audit functionality
/// </summary>
public static class AuditConfigurationExtensions
{
    /// <summary>
    /// Configures automatic audit tracking for entities
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <returns>The audit configuration builder for further configuration</returns>
    public static IAuditConfigurationBuilder WithAudit(this IFSEntityFrameworkBuilder builder)
    {
        return new AuditConfigurationBuilder(builder);
    }
}