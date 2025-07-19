namespace FS.EntityFramework.Library.Domain;

/// <summary>
/// Base class for domain-specific exceptions
/// Represents errors that occur within the domain layer
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Gets the error code for programmatic handling of the exception
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Gets additional details about the exception for debugging
    /// </summary>
    public object? Details { get; }

    /// <summary>
    /// Initializes a new instance of the DomainException class
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="errorCode">The error code (optional, defaults to class name)</param>
    /// <param name="details">Additional error details (optional)</param>
    protected DomainException(string message, string? errorCode = null, object? details = null)
        : base(message)
    {
        ErrorCode = errorCode ?? GetType().Name;
        Details = details;
    }

    /// <summary>
    /// Initializes a new instance of the DomainException class with an inner exception
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="innerException">The inner exception</param>
    /// <param name="errorCode">The error code (optional, defaults to class name)</param>
    /// <param name="details">Additional error details (optional)</param>
    protected DomainException(string message, Exception innerException, string? errorCode = null, object? details = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode ?? GetType().Name;
        Details = details;
    }
}

/// <summary>
/// Exception thrown when an aggregate cannot be found by its identifier
/// </summary>
public class AggregateNotFoundException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the AggregateNotFoundException class
    /// </summary>
    /// <param name="aggregateType">The type of the aggregate</param>
    /// <param name="id">The identifier that was not found</param>
    public AggregateNotFoundException(Type aggregateType, object id)
        : base($"{aggregateType.Name} with ID '{id}' was not found", "AGGREGATE_NOT_FOUND", new { AggregateType = aggregateType.Name, Id = id })
    {
    }
}

/// <summary>
/// Exception thrown when domain invariants are violated
/// </summary>
public class DomainInvariantViolationException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the DomainInvariantViolationException class
    /// </summary>
    /// <param name="message">The error message describing the violated invariant</param>
    /// <param name="details">Additional details about the violation</param>
    public DomainInvariantViolationException(string message, object? details = null)
        : base(message, "DOMAIN_INVARIANT_VIOLATION", details)
    {
    }
}