using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.CommonTests;

/// <summary>
/// Tests for implicit timestamp updates that occur as side effects of file/folder operations.
/// </summary>
public abstract partial class CommonIModifiableFolderTests
{
    /// <summary>
    /// Tests whether opening a file for read updates the file's LastAccessedAt timestamp.
    /// </summary>
    [TestMethod]
    public async Task OpenFileRead_UpdatesLastAccessedAt()
    {
        if (LastAccessedAtUpdateBehavior == PropertyUpdateBehavior.Never)
            return;

        var folder = await CreateModifiableFolderWithItems(1, 0);
        var file = await folder.GetFilesAsync().FirstAsync();

        if (file is not ILastAccessedAt lastAccessedAtFile)
            return;

        // Set LastAccessedAt to the past
        var oldLastAccessedAt = DateTime.UtcNow.AddDays(-7);
        if (lastAccessedAtFile.LastAccessedAt is IModifiableStorageProperty<DateTime?> modifiableProp)
        {
            try { await modifiableProp.UpdateValueAsync(oldLastAccessedAt, CancellationToken.None); }
            catch { return; } // Can't set timestamp, nothing meaningful to test
        }
        else
        {
            return; // Read-only LastAccessedAt, can't backdate to test
        }

        // Capture before value
        var before = await lastAccessedAtFile.LastAccessedAt.GetValueAsync(CancellationToken.None);

        // Open and read the file, then close it
        {
            using var stream = await file.OpenReadAsync();
            var buffer = new byte[1];
            _ = await stream.ReadAsync(buffer, 0, 1);
        }

        // Capture after value (after stream is closed)
        var after = await lastAccessedAtFile.LastAccessedAt.GetValueAsync(CancellationToken.None);

        switch (LastAccessedAtUpdateBehavior)
        {
            case PropertyUpdateBehavior.Immediate:
                Assert.IsTrue(!AreTimestampsEqual(before, after), 
                    $"LastAccessedAt should update immediately after file read. Before={before:O}, After={after:O}");
                break;

            case PropertyUpdateBehavior.Eventual:
                // For eventual updates, we can't strictly assert - the value may or may not have changed yet
                // Just verify we can read the value without error
                break;
        }
    }

    /// <summary>
    /// Tests whether opening a file for write updates the file's LastAccessedAt timestamp.
    /// </summary>
    [TestMethod]
    public async Task OpenFileWrite_UpdatesLastAccessedAt()
    {
        if (LastAccessedAtUpdateBehavior == PropertyUpdateBehavior.Never)
            return;

        var folder = await CreateModifiableFolderWithItems(1, 0);
        var file = await folder.GetFilesAsync().FirstAsync();

        if (file is not ILastAccessedAt lastAccessedAtFile)
            return;

        // Set LastAccessedAt to the past
        var oldLastAccessedAt = DateTime.UtcNow.AddDays(-7);
        if (lastAccessedAtFile.LastAccessedAt is IModifiableStorageProperty<DateTime?> modifiableProp)
        {
            try { await modifiableProp.UpdateValueAsync(oldLastAccessedAt, CancellationToken.None); }
            catch { return; } // Can't set timestamp, nothing meaningful to test
        }
        else
        {
            return; // Read-only LastAccessedAt, can't backdate to test
        }

        // Capture before value
        var before = await lastAccessedAtFile.LastAccessedAt.GetValueAsync(CancellationToken.None);

        // Open for write, then close it
        {
            using var stream = await file.OpenWriteAsync();
            await stream.WriteAsync(new byte[] { 0x42 }, 0, 1);
        }

        // Capture after value (after stream is closed)
        var after = await lastAccessedAtFile.LastAccessedAt.GetValueAsync(CancellationToken.None);

        switch (LastAccessedAtUpdateBehavior)
        {
            case PropertyUpdateBehavior.Immediate:
                Assert.IsTrue(!AreTimestampsEqual(before, after), 
                    $"LastAccessedAt should update immediately after file write. Before={before:O}, After={after:O}");
                break;

            case PropertyUpdateBehavior.Eventual:
                // For eventual updates, we can't strictly assert
                break;
        }
    }

    /// <summary>
    /// Tests whether writing to a file updates the file's LastModifiedAt timestamp.
    /// </summary>
    [TestMethod]
    public async Task WriteFile_UpdatesLastModifiedAt()
    {
        if (LastModifiedAtUpdateBehavior == PropertyUpdateBehavior.Never)
            return;

        var folder = await CreateModifiableFolderWithItems(1, 0);
        var file = await folder.GetFilesAsync().FirstAsync();

        if (file is not ILastModifiedAt lastModifiedAtFile)
            return;

        // Set LastModifiedAt to the past
        var oldLastModifiedAt = DateTime.UtcNow.AddDays(-7);
        if (lastModifiedAtFile.LastModifiedAt is IModifiableStorageProperty<DateTime?> modifiableProp)
        {
            try { await modifiableProp.UpdateValueAsync(oldLastModifiedAt, CancellationToken.None); }
            catch { return; } // Can't set timestamp, nothing meaningful to test
        }
        else
        {
            return; // Read-only LastModifiedAt, can't backdate to test
        }

        // Capture before value
        var before = await lastModifiedAtFile.LastModifiedAt.GetValueAsync(CancellationToken.None);

        // Write to the file, then close it
        {
            using var stream = await file.OpenWriteAsync();
            await stream.WriteAsync(new byte[] { 0x42 }, 0, 1);
        }

        // Capture after value (after stream is closed)
        var after = await lastModifiedAtFile.LastModifiedAt.GetValueAsync(CancellationToken.None);

        switch (LastModifiedAtUpdateBehavior)
        {
            case PropertyUpdateBehavior.Immediate:
                Assert.IsTrue(!AreTimestampsEqual(before, after), 
                    $"LastModifiedAt should update immediately after file write. Before={before:O}, After={after:O}");
                break;

            case PropertyUpdateBehavior.Eventual:
                // For eventual updates, we can't strictly assert
                break;
        }
    }

    /// <summary>
    /// Tests whether iterating folder contents updates the folder's LastAccessedAt timestamp.
    /// </summary>
    [TestMethod]
    public async Task FolderIteration_UpdatesLastAccessedAt()
    {
        if (LastAccessedAtUpdateBehavior == PropertyUpdateBehavior.Never)
            return;

        var folder = await CreateModifiableFolderWithItems(1, 0);

        if (folder is not ILastAccessedAt lastAccessedAtFolder)
            return;

        // Set LastAccessedAt to the past
        var oldLastAccessedAt = DateTime.UtcNow.AddDays(-7);
        if (lastAccessedAtFolder.LastAccessedAt is IModifiableStorageProperty<DateTime?> modifiableProp)
        {
            try { await modifiableProp.UpdateValueAsync(oldLastAccessedAt, CancellationToken.None); }
            catch { return; } // Can't set timestamp, nothing meaningful to test
        }
        else
        {
            return; // Read-only LastAccessedAt, can't backdate to test
        }

        // Capture before value
        var before = await lastAccessedAtFolder.LastAccessedAt.GetValueAsync(CancellationToken.None);

        // Iterate folder contents
        await foreach (var item in folder.GetItemsAsync())
        {
            _ = item.Name; // Force enumeration
        }

        // Capture after value
        var after = await lastAccessedAtFolder.LastAccessedAt.GetValueAsync(CancellationToken.None);

        switch (LastAccessedAtUpdateBehavior)
        {
            case PropertyUpdateBehavior.Immediate:
                Assert.IsTrue(!AreTimestampsEqual(before, after), 
                    $"LastAccessedAt should update immediately after folder iteration. Before={before:O}, After={after:O}");
                break;

            case PropertyUpdateBehavior.Eventual:
                // For eventual updates, we can't strictly assert
                break;
        }
    }

    /// <summary>
    /// Tests whether creating a file in a folder updates the folder's LastModifiedAt timestamp.
    /// </summary>
    [TestMethod]
    public async Task CreateFileInFolder_UpdatesFolderLastModifiedAt()
    {
        if (LastModifiedAtUpdateBehavior == PropertyUpdateBehavior.Never)
            return;

        var folder = await CreateModifiableFolderWithItems(0, 0);

        if (folder is not ILastModifiedAt lastModifiedAtFolder)
            return;

        // Set LastModifiedAt to the past
        var oldLastModifiedAt = DateTime.UtcNow.AddDays(-7);
        if (lastModifiedAtFolder.LastModifiedAt is IModifiableStorageProperty<DateTime?> modifiableProp)
        {
            try { await modifiableProp.UpdateValueAsync(oldLastModifiedAt, CancellationToken.None); }
            catch { return; } // Can't set timestamp, nothing meaningful to test
        }
        else
        {
            return; // Read-only LastModifiedAt, can't backdate to test
        }

        // Capture before value
        var before = await lastModifiedAtFolder.LastModifiedAt.GetValueAsync(CancellationToken.None);

        // Create a file in the folder
        await folder.CreateFileAsync("test_file.txt");

        // Capture after value
        var after = await lastModifiedAtFolder.LastModifiedAt.GetValueAsync(CancellationToken.None);

        switch (LastModifiedAtUpdateBehavior)
        {
            case PropertyUpdateBehavior.Immediate:
                Assert.IsTrue(!AreTimestampsEqual(before, after), 
                    $"LastModifiedAt should update immediately after creating file in folder. Before={before:O}, After={after:O}");
                break;

            case PropertyUpdateBehavior.Eventual:
                // For eventual updates, we can't strictly assert
                break;
        }
    }

    /// <summary>
    /// Tests whether creating a subfolder updates the parent folder's LastModifiedAt timestamp.
    /// </summary>
    [TestMethod]
    public async Task CreateFolderInFolder_UpdatesFolderLastModifiedAt()
    {
        if (LastModifiedAtUpdateBehavior == PropertyUpdateBehavior.Never)
            return;

        var folder = await CreateModifiableFolderWithItems(0, 0);

        if (folder is not ILastModifiedAt lastModifiedAtFolder)
            return;

        // Set LastModifiedAt to the past
        var oldLastModifiedAt = DateTime.UtcNow.AddDays(-7);
        if (lastModifiedAtFolder.LastModifiedAt is IModifiableStorageProperty<DateTime?> modifiableProp)
        {
            try { await modifiableProp.UpdateValueAsync(oldLastModifiedAt, CancellationToken.None); }
            catch { return; } // Can't set timestamp, nothing meaningful to test
        }
        else
        {
            return; // Read-only LastModifiedAt, can't backdate to test
        }

        // Capture before value
        var before = await lastModifiedAtFolder.LastModifiedAt.GetValueAsync(CancellationToken.None);

        // Create a subfolder
        await folder.CreateFolderAsync("test_subfolder");

        // Capture after value
        var after = await lastModifiedAtFolder.LastModifiedAt.GetValueAsync(CancellationToken.None);

        switch (LastModifiedAtUpdateBehavior)
        {
            case PropertyUpdateBehavior.Immediate:
                Assert.IsTrue(!AreTimestampsEqual(before, after), 
                    $"LastModifiedAt should update immediately after creating subfolder. Before={before:O}, After={after:O}");
                break;

            case PropertyUpdateBehavior.Eventual:
                // For eventual updates, we can't strictly assert
                break;
        }
    }

    /// <summary>
    /// Tests whether deleting a file from a folder updates the folder's LastModifiedAt timestamp.
    /// </summary>
    [TestMethod]
    public async Task DeleteFileFromFolder_UpdatesFolderLastModifiedAt()
    {
        if (LastModifiedAtUpdateBehavior == PropertyUpdateBehavior.Never)
            return;

        var folder = await CreateModifiableFolderWithItems(1, 0);
        var file = await folder.GetFilesAsync().FirstAsync();

        if (folder is not ILastModifiedAt lastModifiedAtFolder)
            return;

        // Set LastModifiedAt to the past
        var oldLastModifiedAt = DateTime.UtcNow.AddDays(-7);
        if (lastModifiedAtFolder.LastModifiedAt is IModifiableStorageProperty<DateTime?> modifiableProp)
        {
            try { await modifiableProp.UpdateValueAsync(oldLastModifiedAt, CancellationToken.None); }
            catch { return; } // Can't set timestamp, nothing meaningful to test
        }
        else
        {
            return; // Read-only LastModifiedAt, can't backdate to test
        }

        // Capture before value
        var before = await lastModifiedAtFolder.LastModifiedAt.GetValueAsync(CancellationToken.None);

        // Delete the file
        await folder.DeleteAsync(file);

        // Capture after value
        var after = await lastModifiedAtFolder.LastModifiedAt.GetValueAsync(CancellationToken.None);

        switch (LastModifiedAtUpdateBehavior)
        {
            case PropertyUpdateBehavior.Immediate:
                Assert.IsTrue(!AreTimestampsEqual(before, after), 
                    $"LastModifiedAt should update immediately after deleting file from folder. Before={before:O}, After={after:O}");
                break;

            case PropertyUpdateBehavior.Eventual:
                // For eventual updates, we can't strictly assert
                break;
        }
    }

    /// <summary>
    /// Tests whether deleting a subfolder updates the parent folder's LastModifiedAt timestamp.
    /// </summary>
    [TestMethod]
    public async Task DeleteFolderFromFolder_UpdatesFolderLastModifiedAt()
    {
        if (LastModifiedAtUpdateBehavior == PropertyUpdateBehavior.Never)
            return;

        var folder = await CreateModifiableFolderWithItems(0, 1);
        var subfolder = await folder.GetFoldersAsync().FirstAsync();

        if (folder is not ILastModifiedAt lastModifiedAtFolder)
            return;

        // Set LastModifiedAt to the past
        var oldLastModifiedAt = DateTime.UtcNow.AddDays(-7);
        if (lastModifiedAtFolder.LastModifiedAt is IModifiableStorageProperty<DateTime?> modifiableProp)
        {
            try { await modifiableProp.UpdateValueAsync(oldLastModifiedAt, CancellationToken.None); }
            catch { return; } // Can't set timestamp, nothing meaningful to test
        }
        else
        {
            return; // Read-only LastModifiedAt, can't backdate to test
        }

        // Capture before value
        var before = await lastModifiedAtFolder.LastModifiedAt.GetValueAsync(CancellationToken.None);

        // Delete the subfolder
        await folder.DeleteAsync(subfolder);

        // Capture after value
        var after = await lastModifiedAtFolder.LastModifiedAt.GetValueAsync(CancellationToken.None);

        switch (LastModifiedAtUpdateBehavior)
        {
            case PropertyUpdateBehavior.Immediate:
                Assert.IsTrue(!AreTimestampsEqual(before, after), 
                    $"LastModifiedAt should update immediately after deleting subfolder. Before={before:O}, After={after:O}");
                break;

            case PropertyUpdateBehavior.Eventual:
                // For eventual updates, we can't strictly assert
                break;
        }
    }

    /// <summary>
    /// Tests that LastAccessedAt is updated when the file is opened, not when it is closed.
    /// </summary>
    [TestMethod]
    public async Task OpenFileWrite_UpdatesLastAccessedAt_AtOpenTime()
    {
        if (LastAccessedAtUpdateBehavior == PropertyUpdateBehavior.Never)
            return;

        var folder = await CreateModifiableFolderWithItems(1, 0);
        var file = await folder.GetFilesAsync().FirstAsync();

        if (file is not ILastAccessedAt lastAccessedAtFile)
            return;

        // Set LastAccessedAt to the past
        var oldLastAccessedAt = DateTime.UtcNow.AddDays(-7);
        if (lastAccessedAtFile.LastAccessedAt is IModifiableStorageProperty<DateTime?> modifiableProp)
        {
            try { await modifiableProp.UpdateValueAsync(oldLastAccessedAt, CancellationToken.None); }
            catch { return; }
        }
        else
        {
            return;
        }

        var before = await lastAccessedAtFile.LastAccessedAt.GetValueAsync(CancellationToken.None);

        // Open the stream but do NOT dispose it yet
        var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(new byte[] { 0x42 }, 0, 1);

        // Check atime BEFORE disposal — it should already be updated at open time
        var duringOpen = await lastAccessedAtFile.LastAccessedAt.GetValueAsync(CancellationToken.None);

        stream.Dispose();

        switch (LastAccessedAtUpdateBehavior)
        {
            case PropertyUpdateBehavior.Immediate:
                Assert.IsTrue(!AreTimestampsEqual(before, duringOpen),
                    $"LastAccessedAt should update when the file is opened, before the stream is disposed. Before={before:O}, DuringOpen={duringOpen:O}");
                break;

            case PropertyUpdateBehavior.Eventual:
                break;
        }
    }

    /// <summary>
    /// Compares two DateTime values with tolerance for filesystem precision differences.
    /// </summary>
    protected static bool AreTimestampsEqual(DateTime? source, DateTime? dest)
    {
        if (source is null || dest is null)
            return false;
        
        // Allow 2 second tolerance for filesystem precision differences
        return Math.Abs((source.Value - dest.Value).TotalSeconds) < 2;
    }
}