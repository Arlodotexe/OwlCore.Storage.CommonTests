using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.CommonTests;

/// <summary>
/// Tests for timestamp preservation during copy and move operations.
/// </summary>
public abstract partial class CommonIModifiableFolderTests
{
    /// <summary>
    /// Tests that CreateCopyOfAsync preserves only LastModifiedAt (not CreatedAt or LastAccessedAt).
    /// This matches native System.IO and Windows.Storage copy behavior.
    /// </summary>
    [TestMethod]
    public async Task CreateCopyOfAsync_PropertyPreservation()
    {
        var sourceFolder = await CreateModifiableFolderAsync();
        var destinationFolder = await CreateModifiableFolderAsync();

        // Create a file with known timestamps
        var oldCreatedAt = DateTime.UtcNow.AddDays(-30);
        var oldLastModifiedAt = DateTime.UtcNow.AddDays(-15);
        var oldLastAccessedAt = DateTime.UtcNow.AddDays(-7);

        var originalFile = await CreateFileInFolderWithTimestampsAsync(sourceFolder, oldCreatedAt, oldLastModifiedAt, oldLastAccessedAt);

        // If implementation can't create files with timestamps, nothing to test
        if (originalFile is null)
            return;

        // Capture source properties
        DateTime? sourceCreatedAt = null;
        DateTime? sourceLastModifiedAt = null;
        DateTime? sourceLastAccessedAt = null;

        if (originalFile is ICreatedAt createdAt)
            sourceCreatedAt = await createdAt.CreatedAt.GetValueAsync(CancellationToken.None);
        
        if (originalFile is ILastModifiedAt lastModifiedAt)
            sourceLastModifiedAt = await lastModifiedAt.LastModifiedAt.GetValueAsync(CancellationToken.None);
        
        if (originalFile is ILastAccessedAt lastAccessedAt)
            sourceLastAccessedAt = await lastAccessedAt.LastAccessedAt.GetValueAsync(CancellationToken.None);

        // If no properties are implemented, nothing to test
        if (sourceCreatedAt is null && sourceLastModifiedAt is null && sourceLastAccessedAt is null)
            return;

        // Perform copy
        var copy = await destinationFolder.CreateCopyOfAsync(originalFile, overwrite: true);

        // Assert copy semantics:
        // - CreatedAt: NOT preserved (new file gets current time)
        // - LastModifiedAt: PRESERVED (content age)
        // - LastAccessedAt: NOT preserved (new file gets current time)

        if (sourceCreatedAt is not null && copy is ICreatedAt copyCreatedAt)
        {
            var destValue = await copyCreatedAt.CreatedAt.GetValueAsync(CancellationToken.None);
            var preserved = AreTimestampsEqualForPreservation(sourceCreatedAt.Value, destValue);
            Assert.IsFalse(preserved, $"Copy should NOT preserve CreatedAt. Source={sourceCreatedAt:O}, Dest={destValue:O}");
        }

        if (sourceLastModifiedAt is not null && copy is ILastModifiedAt copyLastModifiedAt)
        {
            var destValue = await copyLastModifiedAt.LastModifiedAt.GetValueAsync(CancellationToken.None);
            var preserved = AreTimestampsEqualForPreservation(sourceLastModifiedAt.Value, destValue);
            Assert.IsTrue(preserved, $"Copy SHOULD preserve LastModifiedAt. Source={sourceLastModifiedAt:O}, Dest={destValue:O}");
        }

        if (sourceLastAccessedAt is not null && copy is ILastAccessedAt copyLastAccessedAt)
        {
            var destValue = await copyLastAccessedAt.LastAccessedAt.GetValueAsync(CancellationToken.None);
            var preserved = AreTimestampsEqualForPreservation(sourceLastAccessedAt.Value, destValue);
            Assert.IsFalse(preserved, $"Copy should NOT preserve LastAccessedAt. Source={sourceLastAccessedAt:O}, Dest={destValue:O}");
        }
    }

    /// <summary>
    /// Tests that MoveFromAsync preserves all timestamps (CreatedAt, LastModifiedAt, LastAccessedAt).
    /// This matches native System.IO and Windows.Storage move behavior.
    /// </summary>
    [TestMethod]
    public async Task MoveFromAsync_PropertyPreservation()
    {
        var sourceFolder = await CreateModifiableFolderAsync();
        var destinationFolder = await CreateModifiableFolderAsync();

        // Create a file with known timestamps
        var oldCreatedAt = DateTime.UtcNow.AddDays(-30);
        var oldLastModifiedAt = DateTime.UtcNow.AddDays(-15);
        var oldLastAccessedAt = DateTime.UtcNow.AddDays(-7);

        var originalFile = await CreateFileInFolderWithTimestampsAsync(sourceFolder, oldCreatedAt, oldLastModifiedAt, oldLastAccessedAt) as IChildFile;

        // If implementation can't create files with timestamps, nothing to test
        if (originalFile is null)
            return;

        // Capture source properties
        DateTime? sourceCreatedAt = null;
        DateTime? sourceLastModifiedAt = null;
        DateTime? sourceLastAccessedAt = null;

        if (originalFile is ICreatedAt createdAt)
            sourceCreatedAt = await createdAt.CreatedAt.GetValueAsync(CancellationToken.None);
        
        if (originalFile is ILastModifiedAt lastModifiedAt)
            sourceLastModifiedAt = await lastModifiedAt.LastModifiedAt.GetValueAsync(CancellationToken.None);
        
        if (originalFile is ILastAccessedAt lastAccessedAt)
            sourceLastAccessedAt = await lastAccessedAt.LastAccessedAt.GetValueAsync(CancellationToken.None);

        // If no properties are implemented, nothing to test
        if (sourceCreatedAt is null && sourceLastModifiedAt is null && sourceLastAccessedAt is null)
            return;

        // Perform move
        var moved = await destinationFolder.MoveFromAsync(originalFile, sourceFolder, overwrite: true);

        // Assert move semantics: ALL timestamps should be preserved

        if (sourceCreatedAt is not null && moved is ICreatedAt movedCreatedAt)
        {
            var destValue = await movedCreatedAt.CreatedAt.GetValueAsync(CancellationToken.None);
            var preserved = AreTimestampsEqualForPreservation(sourceCreatedAt.Value, destValue);
            Assert.IsTrue(preserved, $"Move SHOULD preserve CreatedAt. Source={sourceCreatedAt:O}, Dest={destValue:O}");
        }

        if (sourceLastModifiedAt is not null && moved is ILastModifiedAt movedLastModifiedAt)
        {
            var destValue = await movedLastModifiedAt.LastModifiedAt.GetValueAsync(CancellationToken.None);
            var preserved = AreTimestampsEqualForPreservation(sourceLastModifiedAt.Value, destValue);
            Assert.IsTrue(preserved, $"Move SHOULD preserve LastModifiedAt. Source={sourceLastModifiedAt:O}, Dest={destValue:O}");
        }

        if (sourceLastAccessedAt is not null && moved is ILastAccessedAt movedLastAccessedAt)
        {
            var destValue = await movedLastAccessedAt.LastAccessedAt.GetValueAsync(CancellationToken.None);
            var preserved = AreTimestampsEqualForPreservation(sourceLastAccessedAt.Value, destValue);
            Assert.IsTrue(preserved, $"Move SHOULD preserve LastAccessedAt. Source={sourceLastAccessedAt:O}, Dest={destValue:O}");
        }
    }

    /// <summary>
    /// Compares two DateTime values with tolerance for filesystem precision differences.
    /// </summary>
    private static bool AreTimestampsEqualForPreservation(DateTime? source, DateTime? dest)
    {
        if (source is null || dest is null)
            return false;
        
        // Allow 2 second tolerance for filesystem precision differences
        return Math.Abs((source.Value - dest.Value).TotalSeconds) < 2;
    }
}
