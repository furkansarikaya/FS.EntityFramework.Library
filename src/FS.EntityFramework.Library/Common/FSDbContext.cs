using FS.EntityFramework.Library.FluentConfiguration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.Common;

/// <summary>
/// Base DbContext that automatically applies FS.EntityFramework configurations
/// Now with improved configuration application that actually works
/// </summary>
public abstract class FSDbContext : DbContext
{
    private readonly IServiceProvider? _serviceProvider;

    protected FSDbContext(DbContextOptions options) : base(options)
    {
    }

    protected FSDbContext(DbContextOptions options, IServiceProvider serviceProvider) : base(options)
    {
        _serviceProvider = serviceProvider;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Automatically apply FS.EntityFramework configurations
        ApplyFSConfigurations(modelBuilder);
    }

    /// <summary>
    /// FIXED: Now properly applies configurations without creating meaningless dummy objects
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure</param>
    private void ApplyFSConfigurations(ModelBuilder modelBuilder)
    {
        if (_serviceProvider == null) return;

        // Apply soft delete configuration if registered
        var softDeleteConfig = _serviceProvider.GetService<ISoftDeleteConfiguration>();
        softDeleteConfig?.Configure(modelBuilder);

        // CRITICAL FIX: Don't create dummy objects that serve no purpose
        // The original code created a dummy DbContextOptionsBuilder that was never used
        // Instead, focus on what we CAN do in OnModelCreating: configure the model
        
        // Apply model-level configurations through the configuration applier
        // Only apply configurations that make sense in OnModelCreating context
        var configApplier = _serviceProvider.GetService<IFluentConfigurationApplier>();
        if (configApplier != null)
        {
            // Create a meaningful options builder only if we have configurations that need it
            // In practice, most configurations applied here are model-level, not options-level
            ApplyModelConfigurations(modelBuilder, configApplier);
        }
    }
    
    /// <summary>
    /// Applies configurations that are relevant to model building
    /// This separates model-level configurations from DbContext-level configurations
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    /// <param name="configApplier">The configuration applier</param>
    private void ApplyModelConfigurations(ModelBuilder modelBuilder, IFluentConfigurationApplier configApplier)
    {
        // Apply only model-building configurations
        // DbContext options should be configured when the DbContext is registered, not here
        
        // Soft delete setup - this affects the model
        var softDeleteSetup = _serviceProvider?.GetService<ISoftDeleteSetup>();
        softDeleteSetup?.ApplyConfiguration(modelBuilder);
        
        // Note: We don't apply DbContextOptions here because:
        // 1. OnModelCreating is not the right place for DbContextOptions
        // 2. The DbContext is already created with its options at this point
        // 3. Creating a "dummy" options builder that's not used is misleading
        
        // If we need to apply options-level configurations, they should be applied
        // when the DbContext is registered in DI, not in OnModelCreating
    }

    /// <summary>
    /// IMPROVED: Override OnConfiguring to apply options-level configurations
    /// This is the appropriate place for DbContextOptions modifications
    /// </summary>
    /// <param name="optionsBuilder">The options builder</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Apply options-level configurations if we have a service provider
        if (_serviceProvider == null) return;
        var loggingConfig = _serviceProvider.GetService<IDbContextLoggingConfiguration>();
        loggingConfig?.Configure(optionsBuilder);
    }
}