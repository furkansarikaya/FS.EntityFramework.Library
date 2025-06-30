using Microsoft.EntityFrameworkCore;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Service for applying fluent configurations to DbContext
/// </summary>
public interface IFluentConfigurationApplier
{
    void ApplyConfigurations(ModelBuilder modelBuilder, DbContextOptionsBuilder optionsBuilder);
}