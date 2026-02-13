namespace OwlCore.Storage.CommonTests;

/// <summary>
/// Describes how a storage property updates in response to operations.
/// </summary>
public enum PropertyUpdateBehavior
{
    /// <summary>
    /// The property never updates after initial value is set.
    /// Example: CreatedAt (immutable by definition), or any timestamp on IPFS.
    /// </summary>
    Never,

    /// <summary>
    /// The property is updated by the time the triggering async operation completes.
    /// Example: LastModifiedAt on local filesystem after write.
    /// </summary>
    Immediate,

    /// <summary>
    /// The property updates eventually, but timing is unpredictable.
    /// Example: LastAccessedAt on cloud storage with async metadata propagation.
    /// </summary>
    Eventual,
}