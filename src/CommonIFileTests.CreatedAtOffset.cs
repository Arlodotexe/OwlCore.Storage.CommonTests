using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OwlCore.Storage.CommonTests;

public abstract partial class CommonIFileTests
{
    [TestMethod]
    public async Task CreatedAtOffset_PropertyName_MatchesInterfaceMember()
    {
        var file = await CreateFileAsync();
        if (file is not ICreatedAtOffset createdAtOffset)
            return;

        Assert.AreEqual(nameof(ICreatedAtOffset.CreatedAtOffset), createdAtOffset.CreatedAtOffset.Name);
    }

    [TestMethod]
    public async Task CreatedAtOffset_GetValueAsync_ReturnsValue()
    {
        var file = await CreateFileAsync();
        if (file is not ICreatedAtOffset createdAtOffset)
            return;

        var value = await createdAtOffset.CreatedAtOffset.GetValueAsync(CancellationToken.None);
        Assert.IsNotNull(value);
        Assert.AreNotEqual(DateTimeOffset.MinValue, value.Value);
    }

    [TestMethod]
    public async Task CreatedAtOffset_GetValueAsync_ImmediateCancellation()
    {
        var file = await CreateFileAsync();
        if (file is not ICreatedAtOffset createdAtOffset)
            return;

        var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsException<OperationCanceledException>(() =>
            createdAtOffset.CreatedAtOffset.GetValueAsync(cts.Token));
    }

    [TestMethod]
    public async Task CreatedAtOffset_Watcher_ReturnsNewInstanceEachCall()
    {
        var file = await CreateFileAsync();
        if (file is not ICreatedAtOffset { CreatedAtOffset: IMutableStorageProperty<DateTimeOffset?> prop })
            return;

        using var watcher1 = await prop.GetWatcherAsync(CancellationToken.None);
        using var watcher2 = await prop.GetWatcherAsync(CancellationToken.None);

        Assert.AreNotSame(watcher1, watcher2);
    }

    [TestMethod]
    public async Task CreatedAtOffset_Watcher_PropertyReferenceIsCorrect()
    {
        var file = await CreateFileAsync();
        if (file is not ICreatedAtOffset { CreatedAtOffset: IMutableStorageProperty<DateTimeOffset?> prop })
            return;

        using var watcher = await prop.GetWatcherAsync(CancellationToken.None);

        Assert.AreSame(prop, watcher.Property);
    }

    [TestMethod]
    public async Task CreatedAtOffset_UpdateValueAsync_PersistsChange()
    {
        var file = await CreateFileAsync();
        if (file is not ICreatedAtOffset { CreatedAtOffset: IModifiableStorageProperty<DateTimeOffset?> prop })
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
    public async Task CreatedAtOffset_UpdateValueAsync_NullThrows()
    {
        var file = await CreateFileAsync();
        if (file is not ICreatedAtOffset { CreatedAtOffset: IModifiableStorageProperty<DateTimeOffset?> prop })
            return;

        await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            await prop.UpdateValueAsync(null, CancellationToken.None));
    }

    [TestMethod]
    public async Task CreatedAtOffset_UpdateValueAsync_ImmediateCancellation()
    {
        var file = await CreateFileAsync();
        if (file is not ICreatedAtOffset { CreatedAtOffset: IModifiableStorageProperty<DateTimeOffset?> prop })
            return;

        var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsException<OperationCanceledException>(() =>
            prop.UpdateValueAsync(DateTimeOffset.Now, cts.Token));
    }
}
