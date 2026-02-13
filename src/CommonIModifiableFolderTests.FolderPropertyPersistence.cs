using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.CommonTests;

public abstract partial class CommonIModifiableFolderTests
{
    [TestMethod]
    public async Task Folder_CreatedAt_UpdateValueAsync_PersistsChange()
    {
        var parent = await CreateModifiableFolderWithItems(0, 1);
        var instance1 = await parent.GetFoldersAsync().FirstOrDefaultAsync();
        var instance2 = await parent.GetFoldersAsync().FirstOrDefaultAsync();
        
        if (instance1 is null || instance2 is null)
            return;

        if (instance1 is not ICreatedAt { CreatedAt: IModifiableStorageProperty<DateTime?> prop1 })
            return;
        
        if (instance2 is not ICreatedAt instance2Prop)
            return;

        var initialValue = await instance2Prop.CreatedAt.GetValueAsync(CancellationToken.None);
        var newValue = DateTime.Now.AddDays(-7);
        
        await prop1.UpdateValueAsync(newValue, CancellationToken.None);

        var retrieved = await instance2Prop.CreatedAt.GetValueAsync(CancellationToken.None);
        
        Assert.IsNotNull(retrieved);
        Assert.AreNotEqual(initialValue, retrieved, $"instance2 should see updated value. Initial={initialValue:O}, Retrieved={retrieved:O}, Expected={newValue:O}");
        Assert.AreEqual(newValue.Year, retrieved.Value.Year);
        Assert.AreEqual(newValue.Month, retrieved.Value.Month);
        Assert.AreEqual(newValue.Day, retrieved.Value.Day);
    }

    [TestMethod]
    public async Task Folder_CreatedAtOffset_UpdateValueAsync_PersistsChange()
    {
        var parent = await CreateModifiableFolderWithItems(0, 1);
        var instance1 = await parent.GetFoldersAsync().FirstOrDefaultAsync();
        var instance2 = await parent.GetFoldersAsync().FirstOrDefaultAsync();
        
        if (instance1 is null || instance2 is null)
            return;

        if (instance1 is not ICreatedAtOffset { CreatedAtOffset: IModifiableStorageProperty<DateTimeOffset?> prop1 })
            return;
        
        if (instance2 is not ICreatedAtOffset instance2Prop)
            return;

        var initialValue = await instance2Prop.CreatedAtOffset.GetValueAsync(CancellationToken.None);
        var newValue = DateTimeOffset.Now.AddDays(-7);
        
        await prop1.UpdateValueAsync(newValue, CancellationToken.None);

        var retrieved = await instance2Prop.CreatedAtOffset.GetValueAsync(CancellationToken.None);
        
        Assert.IsNotNull(retrieved);
        Assert.AreNotEqual(initialValue, retrieved, $"instance2 should see updated value. Initial={initialValue:O}, Retrieved={retrieved:O}, Expected={newValue:O}");
        Assert.AreEqual(newValue.UtcDateTime.Year, retrieved.Value.UtcDateTime.Year);
        Assert.AreEqual(newValue.UtcDateTime.Month, retrieved.Value.UtcDateTime.Month);
        Assert.AreEqual(newValue.UtcDateTime.Day, retrieved.Value.UtcDateTime.Day);
    }

    [TestMethod]
    public async Task Folder_LastAccessedAt_UpdateValueAsync_PersistsChange()
    {
        var parent = await CreateModifiableFolderWithItems(0, 1);
        var instance1 = await parent.GetFoldersAsync().FirstOrDefaultAsync();
        var instance2 = await parent.GetFoldersAsync().FirstOrDefaultAsync();
        
        if (instance1 is null || instance2 is null)
            return;

        if (instance1 is not ILastAccessedAt { LastAccessedAt: IModifiableStorageProperty<DateTime?> prop1 })
            return;
        
        if (instance2 is not ILastAccessedAt instance2Prop)
            return;

        var initialValue = await instance2Prop.LastAccessedAt.GetValueAsync(CancellationToken.None);
        var newValue = DateTime.Now.AddDays(-7);
        
        await prop1.UpdateValueAsync(newValue, CancellationToken.None);

        var retrieved = await instance2Prop.LastAccessedAt.GetValueAsync(CancellationToken.None);
        
        Assert.IsNotNull(retrieved);
        Assert.AreNotEqual(initialValue, retrieved, $"instance2 should see updated value. Initial={initialValue:O}, Retrieved={retrieved:O}, Expected={newValue:O}");
        Assert.AreEqual(newValue.Year, retrieved.Value.Year);
        Assert.AreEqual(newValue.Month, retrieved.Value.Month);
        Assert.AreEqual(newValue.Day, retrieved.Value.Day);
    }

    [TestMethod]
    public async Task Folder_LastAccessedAtOffset_UpdateValueAsync_PersistsChange()
    {
        var parent = await CreateModifiableFolderWithItems(0, 1);
        var instance1 = await parent.GetFoldersAsync().FirstOrDefaultAsync();
        var instance2 = await parent.GetFoldersAsync().FirstOrDefaultAsync();
        
        if (instance1 is null || instance2 is null)
            return;

        if (instance1 is not ILastAccessedAtOffset { LastAccessedAtOffset: IModifiableStorageProperty<DateTimeOffset?> prop1 })
            return;
        
        if (instance2 is not ILastAccessedAtOffset instance2Prop)
            return;

        var initialValue = await instance2Prop.LastAccessedAtOffset.GetValueAsync(CancellationToken.None);
        var newValue = DateTimeOffset.Now.AddDays(-7);
        
        await prop1.UpdateValueAsync(newValue, CancellationToken.None);

        var retrieved = await instance2Prop.LastAccessedAtOffset.GetValueAsync(CancellationToken.None);
        
        Assert.IsNotNull(retrieved);
        Assert.AreNotEqual(initialValue, retrieved, $"instance2 should see updated value. Initial={initialValue:O}, Retrieved={retrieved:O}, Expected={newValue:O}");
        Assert.AreEqual(newValue.UtcDateTime.Year, retrieved.Value.UtcDateTime.Year);
        Assert.AreEqual(newValue.UtcDateTime.Month, retrieved.Value.UtcDateTime.Month);
        Assert.AreEqual(newValue.UtcDateTime.Day, retrieved.Value.UtcDateTime.Day);
    }

    [TestMethod]
    public async Task Folder_LastModifiedAt_UpdateValueAsync_PersistsChange()
    {
        var parent = await CreateModifiableFolderWithItems(0, 1);
        var instance1 = await parent.GetFoldersAsync().FirstOrDefaultAsync();
        var instance2 = await parent.GetFoldersAsync().FirstOrDefaultAsync();
        
        if (instance1 is null || instance2 is null)
            return;

        if (instance1 is not ILastModifiedAt { LastModifiedAt: IModifiableStorageProperty<DateTime?> prop1 })
            return;
        
        if (instance2 is not ILastModifiedAt instance2Prop)
            return;

        var initialValue = await instance2Prop.LastModifiedAt.GetValueAsync(CancellationToken.None);
        var newValue = DateTime.Now.AddDays(-7);
        
        await prop1.UpdateValueAsync(newValue, CancellationToken.None);

        var retrieved = await instance2Prop.LastModifiedAt.GetValueAsync(CancellationToken.None);
        
        Assert.IsNotNull(retrieved);
        Assert.AreNotEqual(initialValue, retrieved, $"instance2 should see updated value. Initial={initialValue:O}, Retrieved={retrieved:O}, Expected={newValue:O}");
        Assert.AreEqual(newValue.Year, retrieved.Value.Year);
        Assert.AreEqual(newValue.Month, retrieved.Value.Month);
        Assert.AreEqual(newValue.Day, retrieved.Value.Day);
    }

    [TestMethod]
    public async Task Folder_LastModifiedAtOffset_UpdateValueAsync_PersistsChange()
    {
        var parent = await CreateModifiableFolderWithItems(0, 1);
        var instance1 = await parent.GetFoldersAsync().FirstOrDefaultAsync();
        var instance2 = await parent.GetFoldersAsync().FirstOrDefaultAsync();
        
        if (instance1 is null || instance2 is null)
            return;

        if (instance1 is not ILastModifiedAtOffset { LastModifiedAtOffset: IModifiableStorageProperty<DateTimeOffset?> prop1 })
            return;
        
        if (instance2 is not ILastModifiedAtOffset instance2Prop)
            return;

        var initialValue = await instance2Prop.LastModifiedAtOffset.GetValueAsync(CancellationToken.None);
        var newValue = DateTimeOffset.Now.AddDays(-7);
        
        await prop1.UpdateValueAsync(newValue, CancellationToken.None);

        var retrieved = await instance2Prop.LastModifiedAtOffset.GetValueAsync(CancellationToken.None);
        
        Assert.IsNotNull(retrieved);
        Assert.AreNotEqual(initialValue, retrieved, $"instance2 should see updated value. Initial={initialValue:O}, Retrieved={retrieved:O}, Expected={newValue:O}");
        Assert.AreEqual(newValue.UtcDateTime.Year, retrieved.Value.UtcDateTime.Year);
        Assert.AreEqual(newValue.UtcDateTime.Month, retrieved.Value.UtcDateTime.Month);
        Assert.AreEqual(newValue.UtcDateTime.Day, retrieved.Value.UtcDateTime.Day);
    }
}
