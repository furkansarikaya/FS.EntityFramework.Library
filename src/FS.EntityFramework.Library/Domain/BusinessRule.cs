namespace FS.EntityFramework.Library.Domain;

/// <summary>
/// Base class for implementing business rules with common functionality
/// Provides structure for consistent business rule implementation
/// </summary>
public abstract class BusinessRule : IBusinessRule
{
    /// <summary>
    /// When implemented in a derived class, determines whether this business rule is broken
    /// </summary>
    /// <returns>True if the rule is broken; otherwise, false</returns>
    public abstract bool IsBroken();
        
    /// <summary>
    /// When implemented in a derived class, gets the message describing the broken rule
    /// </summary>
    public abstract string Message { get; }
        
    /// <summary>
    /// Gets the error code for this business rule, defaults to the class name
    /// </summary>
    public virtual string ErrorCode => GetType().Name;
}