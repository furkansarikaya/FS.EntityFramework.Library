using FS.EntityFramework.Library.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Advanced configuration extensions for specialized scenarios
/// </summary>
public static class AdvancedConfigurationExtensions
{
    /// <summary>
    /// Enables OpenTelemetry-compatible production metrics for FS.EntityFramework.Library.
    /// Metrics are collected via System.Diagnostics.Metrics and can be consumed by any
    /// OpenTelemetry-compatible collector. Default: metrics are OFF.
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <returns>The builder for method chaining</returns>
    public static IFSEntityFrameworkBuilder WithMetrics(this IFSEntityFrameworkBuilder builder)
    {
        builder.Services.AddSingleton<FSEntityFrameworkMetrics>();
        return builder;
    }

    /// <summary>
    /// Configures soft delete functionality with global query filters
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <returns>The builder for method chaining</returns>
    public static IFSEntityFrameworkBuilder WithSoftDelete(this IFSEntityFrameworkBuilder builder)
    {
        // Register soft delete configuration as a service so it can be applied in OnModelCreating
        builder.Services.AddSingleton<ISoftDeleteConfiguration, SoftDeleteConfiguration>();
        
        // Register a service that will be used to apply soft delete configuration automatically
        builder.Services.AddScoped<ISoftDeleteSetup, SoftDeleteSetup>();
        
        return builder;
    }

    /// <summary>
    /// Enables detailed logging for Entity Framework operations
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <param name="enableSensitiveDataLogging">Whether to log sensitive data</param>
    /// <returns>The builder for method chaining</returns>
    public static IFSEntityFrameworkBuilder WithDetailedLogging(
        this IFSEntityFrameworkBuilder builder,
        bool enableSensitiveDataLogging = false)
    {
        // Configure logging for the specific DbContext type
        builder.Services.Configure<DbContextLoggerOptions>(options =>
        {
            options.EnableDetailedErrors = true;
            options.EnableSensitiveDataLogging = enableSensitiveDataLogging;
        });

        // Add a configuration service that will apply these settings
        builder.Services.AddScoped<IDbContextLoggingConfiguration, DbContextLoggingConfiguration>();
        
        return builder;
    }
}