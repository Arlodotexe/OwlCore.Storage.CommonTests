using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OwlCore.Storage.CommonTests;

public abstract partial class CommonIFolderTests
{
    [TestMethod]
    public async Task LastModifiedAtOffset_PropertyName_MatchesInterfaceMember()
    {
        var folder = await CreateFolderAsync();
        if (folder is not ILastModifiedAtOffset lastModifiedAtOffset)
            return;

        Assert.AreEqual(nameof(ILastModifiedAtOffset.LastModifiedAtOffset), lastModifiedAtOffset.LastModifiedAtOffset.Name);
    }

    [TestMethod]
    public async Task LastModifiedAtOffset_GetValueAsync_ReturnsValue()
    {
        var folder = await CreateFolderAsync();
        if (folder is not ILastModifiedAtOffset lastModifiedAtOffset)
            return;

        var value = await lastModifiedAtOffset.LastModifiedAtOffset.GetValueAsync(CancellationToken.None);
        Assert.IsNotNull(value);
        Assert.AreNotEqual(DateTimeOffset.MinValue, value.Value);
    }

    [TestMethod]
    public async Task LastModifiedAtOffset_GetValueAsync_ImmediateCancellation()
    {
        var folder = await CreateFolderAsync();
        if (folder is not ILastModifiedAtOffset lastModifiedAtOffset)
            return;

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
            await lastModifiedAtOffset.LastModifiedAtOffset.GetValueAsync(cts.Token));
    }

    [TestMethod]
    public async Task LastModifiedAtOffset_Watcher_ReturnsNewInstanceEachCall()
    {
        var folder = await CreateFolderAsync();
        if (folder is not ILastModifiedAtOffset { LastModifiedAtOffset: IMutableStorageProperty<DateTimeOffset?> prop })
            return;

        using var watcher1 = await prop.GetWatcherAsync(CancellationToken.None);
        using var watcher2 = await prop.GetWatcherAsync(CancellationToken.None);

        Assert.AreNotSame(watcher1, watcher2);
    }

    [TestMethod]
    public async Task LastModifiedAtOffset_Watcher_PropertyReferenceIsCorrect()
    {
        var folder = await CreateFolderAsync();
        if (folder is not ILastModifiedAtOffset { LastModifiedAtOffset: IMutableStorageProperty<DateTimeOffset?> prop })
            return;

        using var watcher = await prop.GetWatcherAsync(CancellationToken.None);

        Assert.AreSame(prop, watcher.Property);
    }

    [TestMethod]
    public async Task LastModifiedAtOffset_UpdateValueAsync_PersistsChange()
    {
        var folder = await CreateFolderAsync();
        if (folder is not ILastModifiedAtOffset { LastModifiedAtOffset: IModifiableStorageProperty<DateTimeOffset?> prop })
            return;

        var newValue = DateTimeOffset.Now.AddDays(-1);
        await prop.UpdateValueAsync(newValue, CancellationToken.None);

        var retrieved = await prop.GetValueAsync(CancellationToken.None);
        Assert.IsNotNull(retrieved);
        Assert.AreEqual(newValue.UtcDateTime.Year, retrieved.Value.UtcDateTime.Year);
        Assert.AreEqual(newValue.UtcDateTime.Month, retrieved.Value.UtcDateTime.Month);
        Assert.AreEqual(newValue.UtcDateTime.Day, retrieved.Value.UtcDateTime.Day);
        Assert.AreEqual(newValue.UtcDateTime.Hour, retrieved.Value.UtcDateTime.Hour);
        Assert.AreEqual(newValue.UtcDateTime.Minute, retrieved.Value.UtcDateTime.Minute);
        Assert.AreEqual(newValue.UtcDateTime.Second, retrieved.Value.UtcDateTime.Second);
    }

    [TestMethod]
    public async Task LastModifiedAtOffset_UpdateValueAsync_NullThrows()
    {
        var folder = await CreateFolderAsync();
        if (folder is not ILastModifiedAtOffset { LastModifiedAtOffset: IModifiableStorageProperty<DateTimeOffset?> prop })
            return;

        await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            await prop.UpdateValueAsync(null, CancellationToken.None));
    }

    [TestMethod]
    public async Task LastModifiedAtOffset_UpdateValueAsync_ImmediateCancellation()
    {
        var folder = await CreateFolderAsync();
        if (folder is not ILastModifiedAtOffset { LastModifiedAtOffset: IModifiableStorageProperty<DateTimeOffset?> prop })
            return;

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
            await prop.UpdateValueAsync(DateTimeOffset.Now, cts.Token));
    }
}
