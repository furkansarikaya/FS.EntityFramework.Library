namespace FS.EntityFramework.Library.Common;

/// <summary>
/// Interface for entities that have creation audit properties
/// Includes CreatedAt and CreatedBy properties
/// </summary>
public interface ICreationAuditableEntity
{
    /// <summary>
    /// Gets or sets the date and time when the entity was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the identifier of the user who created the entity
    /// </summary>
    public string? CreatedBy { get; set; }
}