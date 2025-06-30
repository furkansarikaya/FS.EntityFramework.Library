using Microsoft.EntityFrameworkCore;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Interface for soft delete configuration
/// </summary>
public interface ISoftDeleteConfiguration
{
    /// <summary>
    /// Applies soft delete configuration to the model builder
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    void Configure(ModelBuilder modelBuilder);
}