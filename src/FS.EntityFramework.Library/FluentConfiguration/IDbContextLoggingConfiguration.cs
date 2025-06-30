using Microsoft.EntityFrameworkCore;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Interface for DbContext logging configuration
/// </summary>
public interface IDbContextLoggingConfiguration
{
    void Configure(DbContextOptionsBuilder optionsBuilder);
}