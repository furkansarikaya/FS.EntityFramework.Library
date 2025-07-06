using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FS.EntityFramework.Library.UlidGenerator.Converters;

/// <summary>
/// Custom value converter for ULID (database-agnostic)
/// </summary>
public class UlidConverter() : ValueConverter<Ulid, string>(ulid => ulid.ToString(),
    value => Ulid.Parse(value));