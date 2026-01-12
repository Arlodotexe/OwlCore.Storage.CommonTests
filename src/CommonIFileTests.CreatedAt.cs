using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OwlCore.Storage.CommonTests;

public abstract partial class CommonIFileTests
{
    [TestMethod]
    public async Task CreatedAt_PropertyName_MatchesInterfaceMember()
    {
        var file = await CreateFileAsync();
        if (file is not ICreatedAt createdAt)
            return;

        Assert.AreEqual(nameof(ICreatedAt.CreatedAt), createdAt.CreatedAt.Name);
    }

    [TestMethod]
    public async Task CreatedAt_GetValueAsync_ReturnsValue()
    {
        var file = await CreateFileAsync();
        if (file is not ICreatedAt createdAt)
            return;

        var value = await createdAt.CreatedAt.GetValueAsync(CancellationToken.None);
        Assert.IsNotNull(value);
        Assert.AreNotEqual(DateTime.MinValue, value.Value);
    }

    [TestMethod]
    public async Task CreatedAt_GetValueAsync_ImmediateCancellation()
    {
        var file = await CreateFileAsync();
        if (file is not ICreatedAt createdAt)
            return;

        var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsException<OperationCanceledException>(() =>
            createdAt.CreatedAt.GetValueAsync(cts.Token));
    }

    [TestMethod]
    public async Task CreatedAt_Watcher_ReturnsNewInstanceEachCall()
    {
        var file = await CreateFileAsync();
        if (file is not ICreatedAt { CreatedAt: IMutableStorageProperty<DateTime?> prop })
            return;

        using var watcher1 = await prop.GetWatcherAsync(CancellationToken.None);
        using var watcher2 = await prop.GetWatcherAsync(CancellationToken.None);

        Assert.AreNotSame(watcher1, watcher2);
    }

    [TestMethod]
    public async Task CreatedAt_Watcher_PropertyReferenceIsCorrect()
    {
        var file = await CreateFileAsync();
        if (file is not ICreatedAt { CreatedAt: IMutableStorageProperty<DateTime?> prop })
            return;

        using var watcher = await prop.GetWatcherAsync(CancellationToken.None);

        Assert.AreSame(prop, watcher.Property);
    }

    [TestMethod]
    public async Task CreatedAt_UpdateValueAsync_PersistsChange()
    {
        var file = await CreateFileAsync();
        if (file is not ICreatedAt { CreatedAt: IModifiableStorageProperty<DateTime?> prop })
            return;

        var newValue = DateTime.Now.AddDays(-1);
        await prop.UpdateValueAsync(newValue, CancellationToken.None);

        var retrieved = await prop.GetValueAsync(CancellationToken.None);
        Assert.IsNotNull(retrieved);
        Assert.AreEqual(newValue.Year, retrieved.Value.Year);
        Assert.AreEqual(newValue.Month, retrieved.Value.Month);
        Assert.AreEqual(newValue.Day, retrieved.Value.Day);
        Assert.AreEqual(newValue.Hour, retrieved.Value.Hour);
        Assert.AreEqual(newValue.Minute, retrieved.Value.Minute);
        Assert.AreEqual(newValue.Second, retrieved.Value.Second);
    }

    [TestMethod]
    public async Task CreatedAt_UpdateValueAsync_NullThrows()
    {
        var file = await CreateFileAsync();
        if (file is not ICreatedAt { CreatedAt: IModifiableStorageProperty<DateTime?> prop })
            return;

        await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            await prop.UpdateValueAsync(null, CancellationToken.None));
    }

    [TestMethod]
    public async Task CreatedAt_UpdateValueAsync_ImmediateCancellation()
    {
        var file = await CreateFileAsync();
        if (file is not ICreatedAt { CreatedAt: IModifiableStorageProperty<DateTime?> prop })
            return;

        var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsException<OperationCanceledException>(() =>
            prop.UpdateValueAsync(DateTime.Now, cts.Token));
    }
}
