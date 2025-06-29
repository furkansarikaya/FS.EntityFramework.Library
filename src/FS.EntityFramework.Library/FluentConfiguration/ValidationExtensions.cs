namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Validation extensions for fluent configuration
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Validates the configuration and throws an exception if invalid
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <returns>The builder for method chaining</returns>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
    public static IFSEntityFrameworkBuilder ValidateConfiguration(this IFSEntityFrameworkBuilder builder)
    {
        ValidateDbContext(builder);
        ValidateUnitOfWork(builder);
        return builder;
    }

    /// <summary>
    /// Validates that the DbContext is properly registered
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    private static void ValidateDbContext(IFSEntityFrameworkBuilder builder)
    {
        var dbContextDescriptor = builder.Services.FirstOrDefault(x => x.ServiceType == builder.DbContextType);
        if (dbContextDescriptor == null)
        {
            throw new InvalidOperationException(
                $"DbContext of type {builder.DbContextType.Name} is not registered. " +
                "Please register your DbContext before calling AddFSEntityFramework.");
        }
    }

    /// <summary>
    /// Validates that the UnitOfWork is properly registered
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    private static void ValidateUnitOfWork(IFSEntityFrameworkBuilder builder)
    {
        var unitOfWorkDescriptor = builder.Services.FirstOrDefault(x => 
            x.ServiceType == typeof(UnitOfWorks.IUnitOfWork));
        
        if (unitOfWorkDescriptor == null)
        {
            throw new InvalidOperationException(
                "IUnitOfWork is not registered. This should be automatically registered by AddFSEntityFramework.");
        }
    }
}