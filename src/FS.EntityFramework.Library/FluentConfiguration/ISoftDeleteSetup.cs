using Microsoft.EntityFrameworkCore;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Interface for soft delete setup
/// </summary>
public interface ISoftDeleteSetup
{
    void ApplyConfiguration(ModelBuilder modelBuilder);
}