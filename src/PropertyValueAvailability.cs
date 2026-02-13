namespace OwlCore.Storage.CommonTests;

/// <summary>
/// Describes whether a storage property is expected to have a value.
/// </summary>
public enum PropertyValueAvailability
{
    /// <summary>
    /// The property may or may not have a value depending on state or timing.
    /// Example: LastAccessedAt on OneDrive (null until server populates it).
    /// </summary>
    Maybe,

    /// <summary>
    /// The property always returns a non-null value.
    /// Example: CreatedAt on local filesystem.
    /// </summary>
    Always,

    /// <summary>
    /// The property never has a value (protocol doesn't support it).
    /// Example: LastAccessedAt on FTP (not in FTP protocol).
    /// </summary>
    Never,
}
