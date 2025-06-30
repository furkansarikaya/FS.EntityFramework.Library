using Microsoft.EntityFrameworkCore;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Implementation of soft delete setup
/// </summary>
public class SoftDeleteSetup(ISoftDeleteConfiguration configuration) : ISoftDeleteSetup
{
    public void ApplyConfiguration(ModelBuilder modelBuilder)
    {
        configuration.Configure(modelBuilder);
    }
}