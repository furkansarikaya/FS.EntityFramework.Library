namespace FS.EntityFramework.Library.Domain;

/// <summary>
/// Exception thrown when business rules are violated within the domain
/// Provides structured error handling for domain rule violations
/// </summary>
public class BusinessRuleValidationException : Exception
{
    /// <summary>
    /// Gets the business rule that was broken
    /// </summary>
    public IBusinessRule BrokenRule { get; }
        
    /// <summary>
    /// Gets the error code of the broken business rule
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Initializes a new instance of the BusinessRuleValidationException class
    /// </summary>
    /// <param name="brokenRule">The business rule that was broken</param>
    public BusinessRuleValidationException(IBusinessRule brokenRule) 
        : base(brokenRule.Message)
    {
        BrokenRule = brokenRule;
        ErrorCode = brokenRule.ErrorCode;
    }
}