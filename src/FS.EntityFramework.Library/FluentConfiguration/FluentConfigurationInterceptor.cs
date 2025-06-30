using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Interceptor that applies fluent configurations automatically
/// </summary>
public class FluentConfigurationInterceptor(IServiceProvider serviceProvider) : IInterceptor
{
}