using Microsoft.Extensions.Logging;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Configuration options for DbContext logging
/// </summary>
public class DbContextLoggerOptions
{
    public bool EnableDetailedErrors { get; set; } = true;
    public bool EnableSensitiveDataLogging { get; set; } = false;
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
}