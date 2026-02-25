using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.CommonTests;

/// <summary>
/// Tests that updating one timestamp property via the modifiable property interface
/// does not alter other timestamp properties as a side effect.
/// </summary>
/// <remarks>
/// On Linux, .NET's FileInfo timestamp setters call utimensat(2) with both atime and mtime
/// atomically. Without care, setting one timestamp can silently overwrite another.
/// </remarks>
public abstract partial class CommonIModifiableFolderTests
{
    /// <summary>
    /// Tests that setting LastModifiedAt does not alter LastAccessedAt.
    /// </summary>
    [TestMethod]
    public async Task File_SetLastModifiedAt_DoesNotUpdateLastAccessedAt()
    {
        var parent = await CreateModifiableFolderWithItems(1, 0);
        var file = await parent.GetFilesAsync().FirstOrDefaultAsync();

        if (file is null)
            return;

        if (file is not ILastModifiedAt { LastModifiedAt: IModifiableStorageProperty<DateTime?> modifiableLastModified })
            return;

        if (file is not ILastAccessedAt lastAccessedAtFile)
            return;

        var accessedBefore = await lastAccessedAtFile.LastAccessedAt.GetValueAsync(CancellationToken.None);

        await modifiableLastModified.UpdateValueAsync(DateTime.UtcNow.AddDays(-7), CancellationToken.None);

        var accessedAfter = await lastAccessedAtFile.LastAccessedAt.GetValueAsync(CancellationToken.None);

        Assert.IsTrue(AreTimestampsEqual(accessedBefore, accessedAfter),
            $"Setting LastModifiedAt should not alter LastAccessedAt. Before={accessedBefore:O}, After={accessedAfter:O}");
    }

    /// <summary>
    /// Tests that setting LastAccessedAt does not alter LastModifiedAt.
    /// </summary>
    [TestMethod]
    public async Task File_SetLastAccessedAt_DoesNotUpdateLastModifiedAt()
    {
        var parent = await CreateModifiableFolderWithItems(1, 0);
        var file = await parent.GetFilesAsync().FirstOrDefaultAsync();

        if (file is null)
            return;

        if (file is not ILastAccessedAt { LastAccessedAt: IModifiableStorageProperty<DateTime?> modifiableLastAccessed })
            return;

        if (file is not ILastModifiedAt lastModifiedAtFile)
            return;

        var modifiedBefore = await lastModifiedAtFile.LastModifiedAt.GetValueAsync(CancellationToken.None);

        await modifiableLastAccessed.UpdateValueAsync(DateTime.UtcNow.AddDays(-7), CancellationToken.None);

        var modifiedAfter = await lastModifiedAtFile.LastModifiedAt.GetValueAsync(CancellationToken.None);

        Assert.IsTrue(AreTimestampsEqual(modifiedBefore, modifiedAfter),
            $"Setting LastAccessedAt should not alter LastModifiedAt. Before={modifiedBefore:O}, After={modifiedAfter:O}");
    }

    /// <summary>
    /// Tests that setting CreatedAt does not alter LastModifiedAt.
    /// </summary>
    /// <remarks>
    /// On Linux, there is no native creation time — setting CreatedAt maps to SetLastWriteTime
    /// internally, which would overwrite mtime if not handled carefully.
    /// </remarks>
    [TestMethod]
    public async Task File_SetCreatedAt_DoesNotUpdateLastModifiedAt()
    {
        var parent = await CreateModifiableFolderWithItems(1, 0);
        var file = await parent.GetFilesAsync().FirstOrDefaultAsync();

        if (file is null)
            return;

        if (file is not ICreatedAt { CreatedAt: IModifiableStorageProperty<DateTime?> modifiableCreatedAt })
            return;

        if (file is not ILastModifiedAt lastModifiedAtFile)
            return;

        var modifiedBefore = await lastModifiedAtFile.LastModifiedAt.GetValueAsync(CancellationToken.None);

        try { await modifiableCreatedAt.UpdateValueAsync(DateTime.UtcNow.AddDays(-7), CancellationToken.None); }
        catch { return; } // Setting CreatedAt is not supported on all platforms/filesystems

        var modifiedAfter = await lastModifiedAtFile.LastModifiedAt.GetValueAsync(CancellationToken.None);

        Assert.IsTrue(AreTimestampsEqual(modifiedBefore, modifiedAfter),
            $"Setting CreatedAt should not alter LastModifiedAt. Before={modifiedBefore:O}, After={modifiedAfter:O}");
    }

    /// <summary>
    /// Tests that setting CreatedAt does not alter LastAccessedAt.
    /// </summary>
    [TestMethod]
    public async Task File_SetCreatedAt_DoesNotUpdateLastAccessedAt()
    {
        var parent = await CreateModifiableFolderWithItems(1, 0);
        var file = await parent.GetFilesAsync().FirstOrDefaultAsync();

        if (file is null)
            return;

        if (file is not ICreatedAt { CreatedAt: IModifiableStorageProperty<DateTime?> modifiableCreatedAt })
            return;

        if (file is not ILastAccessedAt lastAccessedAtFile)
            return;

        var accessedBefore = await lastAccessedAtFile.LastAccessedAt.GetValueAsync(CancellationToken.None);

        try { await modifiableCreatedAt.UpdateValueAsync(DateTime.UtcNow.AddDays(-7), CancellationToken.None); }
        catch { return; }

        var accessedAfter = await lastAccessedAtFile.LastAccessedAt.GetValueAsync(CancellationToken.None);

        Assert.IsTrue(AreTimestampsEqual(accessedBefore, accessedAfter),
            $"Setting CreatedAt should not alter LastAccessedAt. Before={accessedBefore:O}, After={accessedAfter:O}");
    }
}
