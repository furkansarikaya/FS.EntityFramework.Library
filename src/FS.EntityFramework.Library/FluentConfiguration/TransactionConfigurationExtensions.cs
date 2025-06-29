using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Configuration extensions for transaction management
/// </summary>
public static class TransactionConfigurationExtensions
{
    /// <summary>
    /// Configures automatic transaction management for Unit of Work
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <param name="isolationLevel">The default isolation level for transactions</param>
    /// <returns>The builder for method chaining</returns>
    public static IFSEntityFrameworkBuilder WithAutoTransactions(
        this IFSEntityFrameworkBuilder builder,
        System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted)
    {
        builder.Services.Configure<TransactionOptions>(options =>
        {
            options.DefaultIsolationLevel = isolationLevel;
            options.EnableAutoTransactions = true;
        });

        return builder;
    }

    /// <summary>
    /// Configures transaction timeout settings
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <param name="timeout">The transaction timeout duration</param>
    /// <returns>The builder for method chaining</returns>
    public static IFSEntityFrameworkBuilder WithTransactionTimeout(
        this IFSEntityFrameworkBuilder builder,
        TimeSpan timeout)
    {
        builder.Services.Configure<TransactionOptions>(options =>
        {
            options.DefaultTimeout = timeout;
        });

        return builder;
    }
}