namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Configuration options for transaction management
/// </summary>
public class TransactionOptions
{
    /// <summary>
    /// Gets or sets the default isolation level for transactions
    /// </summary>
    public System.Data.IsolationLevel DefaultIsolationLevel { get; set; } = System.Data.IsolationLevel.ReadCommitted;

    /// <summary>
    /// Gets or sets whether automatic transactions are enabled
    /// </summary>
    public bool EnableAutoTransactions { get; set; } = false;

    /// <summary>
    /// Gets or sets the default transaction timeout
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(2);
}