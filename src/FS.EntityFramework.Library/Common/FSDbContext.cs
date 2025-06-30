using FS.EntityFramework.Library.FluentConfiguration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.Common;

/// <summary>
/// Base DbContext that automatically applies FS.EntityFramework configurations
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

    private void ApplyFSConfigurations(ModelBuilder modelBuilder)
    {
        if (_serviceProvider == null) return;

        // Apply soft delete configuration if registered
        var softDeleteConfig = _serviceProvider.GetService<ISoftDeleteConfiguration>();
        softDeleteConfig?.Configure(modelBuilder);

        // Apply other configurations via the applier
        var configApplier = _serviceProvider.GetService<IFluentConfigurationApplier>();
        if (configApplier == null) return;
        var dummyOptionsBuilder = new DbContextOptionsBuilder();
        configApplier.ApplyConfigurations(modelBuilder, dummyOptionsBuilder);
    }
}
