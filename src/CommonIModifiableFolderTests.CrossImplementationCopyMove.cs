using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.CommonTests;

/// <summary>
/// Tests for cross-implementation copy and move operations.
/// Tests fallback paths that copy timestamps when bridging different storage implementations.
/// </summary>
public abstract partial class CommonIModifiableFolderTests
{
    /// <summary>
    /// Tests copying FROM the real implementation (with known timestamp) TO a mock with timestamp support.
    /// Verifies fallback path preserves only LastModifiedAt (copy semantics).
    /// </summary>
    [TestMethod]
    public async Task CreateCopyOfAsync_ToMockWithTimestamps_PreservesLastModifiedAt()
    {
        var sourceFolder = await CreateModifiableFolderWithItems(0, 0);
        var expectedTimestamp = DateTime.UtcNow.AddDays(-15);
        
        // Create source file with known timestamp
        var sourceFile = await CreateFileInFolderWithLastModifiedAtAsync(sourceFolder, expectedTimestamp);
        
        // Skip if implementation doesn't support creating files with known timestamps
        if (sourceFile is null)
            return;

        // Dest: mock that forces fallback path (no ICreateCopyOf)
        var destFolder = new MockStorageFolder("mock_dest", null, createFilesWithTimestamps: true);

        // Act - uses fallback path
        var copy = await destFolder.CreateCopyOfAsync(sourceFile, overwrite: true);

        // Assert - only LastModifiedAt should be preserved (copy semantics)
        Assert.IsInstanceOfType(copy, typeof(MockStorageFile));
        var mockCopy = (MockStorageFile)copy;

        AssertTimestampPreservedUtc(expectedTimestamp, mockCopy.LastModifiedAtValue?.UtcDateTime, "LastModifiedAt");
    }

    /// <summary>
    /// Tests copying FROM a mock with timestamps TO the real implementation.
    /// Verifies fallback path preserves only LastModifiedAt (copy semantics) when destination supports it.
    /// </summary>
    /// <remarks>
    /// Copy semantics: The copy extension reads timestamps from source and attempts to write them to destination.
    /// If the destination property is read-only (not IModifiableStorageProperty), the extension cannot set the value,
    /// so we only assert preservation when the destination explicitly supports modifiable timestamps.
    /// This accommodates implementations like archives where entry timestamps are determined at creation time
    /// and cannot be modified after the fact.
    /// </remarks>
    [TestMethod]
    public async Task CreateCopyOfAsync_FromMockWithTimestamps_PreservesLastModifiedAt()
    {
        // Source: mock with timestamps
        var sourceFolder = MockStorageFolder.CreateWithItems("mock_source", 1, 0, filesWithTimestamps: true);
        var sourceFile = (MockStorageFile)await sourceFolder.GetFilesAsync().FirstAsync();

        // Set backdated timestamps
        var oldLastModifiedAt = DateTimeOffset.UtcNow.AddDays(-15);
        sourceFile.LastModifiedAtValue = oldLastModifiedAt;

        // Dest: real implementation
        var destFolder = await CreateModifiableFolderWithItems(0, 0);

        // Act - uses fallback path
        var copy = await destFolder.CreateCopyOfAsync(sourceFile, overwrite: true);

        // Assert - only LastModifiedAt should be preserved (copy semantics).
        // We can only assert preservation if the destination property is modifiable,
        // because otherwise the copy extension had no way to set the timestamp.
        // Read-only timestamp implementations (e.g., archives) will skip this assertion
        // but the copy operation itself still succeeds.
        if (copy is ILastModifiedAt { LastModifiedAt: IModifiableStorageProperty<DateTime?> })
        {
            var destLastModifiedAt = await ((ILastModifiedAt)copy).LastModifiedAt.GetValueAsync(CancellationToken.None);
            AssertTimestampPreservedUtc(oldLastModifiedAt.UtcDateTime, destLastModifiedAt?.ToUniversalTime(), "LastModifiedAt");
        }
    }

    /// <summary>
    /// Tests copying FROM a mock without timestamps TO the real implementation.
    /// Verifies copy succeeds even when source has no timestamp properties.
    /// </summary>
    [TestMethod]
    public async Task CreateCopyOfAsync_FromMockWithoutTimestamps_Succeeds()
    {
        // Source: mock WITHOUT timestamps
        var sourceFolder = MockStorageFolder.CreateWithItems("mock_source", 1, 0, filesWithTimestamps: false);
        var sourceFile = await sourceFolder.GetFilesAsync().FirstAsync();
        Assert.IsInstanceOfType(sourceFile, typeof(MockStorageFileNoTimestamps));

        // Dest: real implementation
        var destFolder = await CreateModifiableFolderWithItems(0, 0);

        // Act - should succeed without throwing
        var copy = await destFolder.CreateCopyOfAsync(sourceFile, overwrite: true);

        // Assert - copy succeeded
        Assert.IsNotNull(copy);
        Assert.AreEqual(sourceFile.Name, copy.Name);
    }

    /// <summary>
    /// Tests moving FROM the real implementation (with known timestamps) TO a mock with timestamp support.
    /// Verifies fallback move (copy + delete) preserves ALL timestamps (move semantics).
    /// </summary>
    [TestMethod]
    public async Task MoveFromAsync_ToMockWithTimestamps_PreservesAllTimestamps()
    {
        var sourceFolder = await CreateModifiableFolderWithItems(0, 0);
        
        var expectedCreatedAt = DateTime.UtcNow.AddDays(-30);
        var expectedLastModifiedAt = DateTime.UtcNow.AddDays(-15);
        var expectedLastAccessedAt = DateTime.UtcNow.AddDays(-7);
        
        // Create source file with known timestamps
        var createdFileData = await CreateFileInFolderWithTimestampsAsync(sourceFolder, expectedCreatedAt, expectedLastModifiedAt, expectedLastAccessedAt);
        var sourceFile = createdFileData?.CreatedFile;
        
        // Skip if implementation doesn't support creating files with known timestamps
        if (createdFileData is null)
            return;

        if (sourceFile is not IChildFile childFile)
            return;

        // Dest: mock that forces fallback path
        var destFolder = new MockStorageFolder("mock_dest", null, createFilesWithTimestamps: true);

        // Act - uses fallback path (copy + delete)
        var moved = await destFolder.MoveFromAsync(childFile, sourceFolder, overwrite: true);

        // Assert - ALL timestamps should be preserved (move semantics)
        Assert.IsInstanceOfType(moved, typeof(MockStorageFile));
        var mockMoved = (MockStorageFile)moved;

        // Check each timestamp - only assert if source had it
        if (sourceFile is ICreatedAt && createdFileData?.CreatedAt is not null)
            AssertTimestampPreservedUtc(expectedCreatedAt, mockMoved.CreatedAtValue?.UtcDateTime, "CreatedAt");
        
        if (sourceFile is ILastModifiedAt && createdFileData?.LastModifiedAt is not null)
            AssertTimestampPreservedUtc(expectedLastModifiedAt, mockMoved.LastModifiedAtValue?.UtcDateTime, "LastModifiedAt");

        if (sourceFile is ILastAccessedAt && createdFileData?.LastAccessedAt is not null)
            AssertTimestampPreservedUtc(expectedLastAccessedAt, mockMoved.LastAccessedAtValue?.UtcDateTime, "LastAccessedAt");

        // Source should be deleted
        var remaining = await sourceFolder.GetFilesAsync().ToListAsync();
        Assert.AreEqual(0, remaining.Count, "Source file should be deleted after move");
    }

    /// <summary>
    /// Tests moving FROM a mock with timestamps TO the real implementation.
    /// Verifies fallback move preserves ALL timestamps (move semantics) when destination supports it.
    /// </summary>
    /// <remarks>
    /// Move semantics: The move extension reads all timestamps from source and attempts to write them to destination.
    /// If the destination properties are read-only (not IModifiableStorageProperty), the extension cannot set the values,
    /// so we only assert preservation when the destination explicitly supports modifiable timestamps.
    /// This accommodates implementations like archives where entry timestamps are determined at creation time
    /// and cannot be modified after the fact.
    /// </remarks>
    [TestMethod]
    public async Task MoveFromAsync_FromMockWithTimestamps_PreservesAllTimestamps()
    {
        // Source: mock with timestamps
        var sourceFolder = MockStorageFolder.CreateWithItems("mock_source", 1, 0, filesWithTimestamps: true);
        var sourceFile = (MockStorageFile)await sourceFolder.GetFilesAsync().FirstAsync();

        var oldCreatedAt = DateTimeOffset.UtcNow.AddDays(-30);
        var oldLastModifiedAt = DateTimeOffset.UtcNow.AddDays(-15);
        var oldLastAccessedAt = DateTimeOffset.UtcNow.AddDays(-7);
        sourceFile.CreatedAtValue = oldCreatedAt;
        sourceFile.LastModifiedAtValue = oldLastModifiedAt;
        sourceFile.LastAccessedAtValue = oldLastAccessedAt;

        // Dest: real implementation
        var destFolder = await CreateModifiableFolderWithItems(0, 0);

        // Act - uses fallback path
        var moved = await destFolder.MoveFromAsync(sourceFile, sourceFolder, overwrite: true);

        // Assert - timestamps should be preserved only if destination supports modifiable properties.
        // We can only assert preservation if the destination property is modifiable,
        // because otherwise the move extension had no way to set the timestamp.
        // Read-only timestamp implementations (e.g., archives) will skip these assertions
        // but the move operation itself still succeeds.
        
        if (moved is ICreatedAt { CreatedAt: IModifiableStorageProperty<DateTime?> })
        {
            var destCreatedAt = await ((ICreatedAt)moved).CreatedAt.GetValueAsync(CancellationToken.None);
            AssertTimestampPreservedUtc(oldCreatedAt.UtcDateTime, destCreatedAt?.ToUniversalTime(), "CreatedAt");
        }

        if (moved is ILastModifiedAt { LastModifiedAt: IModifiableStorageProperty<DateTime?> })
        {
            var destLastModifiedAt = await ((ILastModifiedAt)moved).LastModifiedAt.GetValueAsync(CancellationToken.None);
            AssertTimestampPreservedUtc(oldLastModifiedAt.UtcDateTime, destLastModifiedAt?.ToUniversalTime(), "LastModifiedAt");
        }

        if (moved is ILastAccessedAt { LastAccessedAt: IModifiableStorageProperty<DateTime?> })
        {
            var destLastAccessedAt = await ((ILastAccessedAt)moved).LastAccessedAt.GetValueAsync(CancellationToken.None);
            AssertTimestampPreservedUtc(oldLastAccessedAt.UtcDateTime, destLastAccessedAt?.ToUniversalTime(), "LastAccessedAt");
        }

        // Source should be deleted
        var remaining = await sourceFolder.GetFilesAsync().ToListAsync();
        Assert.AreEqual(0, remaining.Count, "Source file should be deleted after move");
    }

    /// <summary>
    /// Tests that file content is preserved during cross-implementation copy.
    /// </summary>
    [TestMethod]
    public async Task CreateCopyOfAsync_CrossImplementation_PreservesContent()
    {
        // Source: mock with known content
        var sourceFolder = new MockStorageFolder("mock_source");
        var expectedContent = global::System.Text.Encoding.UTF8.GetBytes("Cross-implementation content test!");
        var sourceFile = new MockStorageFile("content_test.txt", sourceFolder, expectedContent);
        sourceFolder.AddChild(sourceFile);

        // Dest: real implementation
        var destFolder = await CreateModifiableFolderWithItems(0, 0);

        // Act
        var copy = await destFolder.CreateCopyOfAsync(sourceFile, overwrite: true);

        // Assert
        using var stream = await copy.OpenStreamAsync(FileAccess.Read);
        var actualContent = new byte[stream.Length];
        await stream.ReadAsync(actualContent, 0, actualContent.Length);

        CollectionAssert.AreEqual(expectedContent, actualContent, "File content should be preserved during cross-implementation copy");
    }

    private static void AssertTimestampPreservedUtc(DateTime? expected, DateTime? actual, string propertyName)
    {
        Assert.IsNotNull(actual, $"{propertyName} should not be null on destination");
        Assert.IsNotNull(expected, $"Expected {propertyName} should not be null");

        var diff = Math.Abs((actual.Value - expected.Value).TotalSeconds);
        Assert.IsTrue(diff < 2,
            $"{propertyName} should be preserved. Expected={expected:O}, Actual={actual:O}, Diff={diff:F2}s");
    }
}
