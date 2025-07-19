namespace FS.EntityFramework.Library.Domain;

/// <summary>
/// Interface for business rules that can be validated within the domain
/// Business rules encapsulate domain logic and invariants
/// </summary>
public interface IBusinessRule
{
    /// <summary>
    /// Determines whether this business rule is currently broken
    /// </summary>
    /// <returns>True if the rule is broken; otherwise, false</returns>
    bool IsBroken();
        
    /// <summary>
    /// Gets the message describing why the business rule is broken
    /// </summary>
    string Message { get; }
        
    /// <summary>
    /// Gets the error code for this business rule for programmatic handling
    /// </summary>
    string ErrorCode { get; }
}