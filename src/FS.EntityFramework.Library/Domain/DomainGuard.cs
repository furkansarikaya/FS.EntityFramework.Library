namespace FS.EntityFramework.Library.Domain;

/// <summary>
/// Enhanced Guard class providing comprehensive validation for domain entities
/// Ensures domain invariants and prevents invalid state transitions
/// </summary>
public static class DomainGuard
{
    /// <summary>
    /// Checks a business rule and throws exception if broken
    /// </summary>
    /// <param name="rule">The business rule to check</param>
    /// <exception cref="BusinessRuleValidationException">Thrown when the rule is broken</exception>
    public static void Against(IBusinessRule rule)
    {
        if (rule.IsBroken())
        {
            throw new BusinessRuleValidationException(rule);
        }
    }

    /// <summary>
    /// Checks multiple business rules and throws exception if any are broken
    /// </summary>
    /// <param name="rules">The business rules to check</param>
    /// <exception cref="BusinessRuleValidationException">Thrown when any rule is broken</exception>
    public static void Against(params IBusinessRule[] rules)
    {
        foreach (var rule in rules)
        {
            Against(rule);
        }
    }

    /// <summary>
    /// Throws exception if the specified condition is true
    /// </summary>
    /// <param name="condition">The condition to check</param>
    /// <param name="message">The error message</param>
    /// <param name="errorCode">The error code (optional)</param>
    /// <exception cref="BusinessRuleValidationException">Thrown when condition is true</exception>
    public static void Against(bool condition, string message, string? errorCode = null)
    {
        if (condition)
        {
            throw new BusinessRuleValidationException(
                new GenericBusinessRule(message, errorCode ?? "DOMAIN_RULE_VIOLATION"));
        }
    }

    /// <summary>
    /// Throws ArgumentNullException if the value is null
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <param name="value">The value to check</param>
    /// <param name="parameterName">The name of the parameter</param>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static void AgainstNull<T>(T? value, string parameterName) where T : class
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }
    }

    /// <summary>
    /// Throws ArgumentException if the string value is null or empty
    /// </summary>
    /// <param name="value">The string value to check</param>
    /// <param name="parameterName">The name of the parameter</param>
    /// <exception cref="ArgumentException">Thrown when value is null or empty</exception>
    public static void AgainstNullOrEmpty(string? value, string parameterName)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("Value cannot be null or empty", parameterName);
        }
    }

    /// <summary>
    /// Throws ArgumentException if the string value is null, empty, or whitespace
    /// </summary>
    /// <param name="value">The string value to check</param>
    /// <param name="parameterName">The name of the parameter</param>
    /// <exception cref="ArgumentException">Thrown when value is null, empty, or whitespace</exception>
    public static void AgainstNullOrWhiteSpace(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null, empty, or whitespace", parameterName);
        }
    }

    /// <summary>
    /// Throws ArgumentException if the numeric value is negative
    /// </summary>
    /// <param name="value">The value to check</param>
    /// <param name="parameterName">The name of the parameter</param>
    /// <exception cref="ArgumentException">Thrown when value is negative</exception>
    public static void AgainstNegative(decimal value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentException("Value cannot be negative", parameterName);
        }
    }

    /// <summary>
    /// Throws ArgumentException if the numeric value is negative or zero
    /// </summary>
    /// <param name="value">The value to check</param>
    /// <param name="parameterName">The name of the parameter</param>
    /// <exception cref="ArgumentException">Thrown when value is negative or zero</exception>
    public static void AgainstNegativeOrZero(decimal value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentException("Value must be positive", parameterName);
        }
    }

    /// <summary>
    /// Throws ArgumentOutOfRangeException if the value is outside the specified range
    /// </summary>
    /// <typeparam name="T">The type of the value (must implement IComparable)</typeparam>
    /// <param name="value">The value to check</param>
    /// <param name="min">The minimum allowed value</param>
    /// <param name="max">The maximum allowed value</param>
    /// <param name="parameterName">The name of the parameter</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is outside range</exception>
    public static void AgainstOutOfRange<T>(T value, T min, T max, string parameterName)
        where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
        {
            throw new ArgumentOutOfRangeException(parameterName,
                $"Value must be between {min} and {max}");
        }
    }

    /// <summary>
    /// Throws ArgumentException if the collection is null or empty
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    /// <param name="collection">The collection to check</param>
    /// <param name="parameterName">The name of the parameter</param>
    /// <exception cref="ArgumentException">Thrown when collection is null or empty</exception>
    public static void AgainstNullOrEmpty<T>(IEnumerable<T>? collection, string parameterName)
    {
        if (collection == null || !collection.Any())
        {
            throw new ArgumentException("Collection cannot be null or empty", parameterName);
        }
    }

    /// <summary>
    /// Internal generic business rule for simple validations
    /// </summary>
    private class GenericBusinessRule(string message, string errorCode) : BusinessRule
    {
        public override bool IsBroken() => true;
        public override string Message { get; } = message;
        public override string ErrorCode { get; } = errorCode;
    }
}