using FS.EntityFramework.Library.FluentConfiguration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.Extensions;

/// <summary>
/// Extension methods for DbContext to apply fluent configurations
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// Applies all fluent configurations registered through AddFSEntityFramework
    /// Call this in your DbContext's OnModelCreating method
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    /// <param name="serviceProvider">The service provider (usually from DI container)</param>
    public static void ApplyFSEntityFrameworkConfigurations(this ModelBuilder modelBuilder, IServiceProvider serviceProvider)
    {
        var configurationApplier = serviceProvider.CreateScope().ServiceProvider.GetService<IFluentConfigurationApplier>();
        if (configurationApplier == null) return;
        // We can't pass DbContextOptionsBuilder here since OnModelCreating doesn't have access to it
        // For now, we'll only apply ModelBuilder configurations
        var dummyOptionsBuilder = new DbContextOptionsBuilder();
        configurationApplier.ApplyConfigurations(modelBuilder, dummyOptionsBuilder);
    }

    /// <summary>
    /// Applies all fluent configurations registered through AddFSEntityFramework
    /// Call this in your DbContext's OnConfiguring method
    /// </summary>
    /// <param name="optionsBuilder">The options builder</param>
    /// <param name="serviceProvider">The service provider (usually from DI container)</param>
    public static void ApplyFSEntityFrameworkLogging(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider)
    {
        var loggingConfig = serviceProvider.CreateScope().ServiceProvider.GetService<IDbContextLoggingConfiguration>();
        loggingConfig?.Configure(optionsBuilder);
    }
}