using FS.EntityFramework.Library.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Default implementation of soft delete configuration
/// </summary>
public class SoftDeleteConfiguration : ISoftDeleteConfiguration
{
    /// <summary>
    /// Applies soft delete query filters to all applicable entities
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplySoftDeleteQueryFilters();
    }
}