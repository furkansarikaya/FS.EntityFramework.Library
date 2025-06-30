using Microsoft.EntityFrameworkCore;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Implementation of DbContext logging configuration
/// </summary>
public class DbContextLoggingConfiguration(Microsoft.Extensions.Options.IOptions<DbContextLoggerOptions> options)
    : IDbContextLoggingConfiguration
{
    private readonly DbContextLoggerOptions _options = options.Value;

    public void Configure(DbContextOptionsBuilder optionsBuilder)
    {
        if (_options.EnableDetailedErrors)
        {
            optionsBuilder.EnableDetailedErrors();
        }

        if (_options.EnableSensitiveDataLogging)
        {
            optionsBuilder.EnableSensitiveDataLogging();
        }

        optionsBuilder.LogTo(Console.WriteLine, _options.MinimumLevel);
    }
}