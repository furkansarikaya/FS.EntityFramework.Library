using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Implementation of fluent configuration applier
/// </summary>
public class FluentConfigurationApplier(IServiceProvider serviceProvider) : IFluentConfigurationApplier
{
    public void ApplyConfigurations(ModelBuilder modelBuilder, DbContextOptionsBuilder optionsBuilder)
    {
        // Apply soft delete configuration if registered
        var softDeleteSetup = serviceProvider.CreateScope().ServiceProvider.GetService<ISoftDeleteSetup>();
        softDeleteSetup?.ApplyConfiguration(modelBuilder);

        // Apply logging configuration if registered
        var loggingConfig = serviceProvider.CreateScope().ServiceProvider.GetService<IDbContextLoggingConfiguration>();
        loggingConfig?.Configure(optionsBuilder);
    }
}