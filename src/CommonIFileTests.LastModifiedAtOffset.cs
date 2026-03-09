using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OwlCore.Storage.CommonTests;

public abstract partial class CommonIFileTests
{
    [TestMethod]
    public async Task LastModifiedAtOffset_PropertyName_MatchesInterfaceMember()
    {
        var file = await CreateFileAsync();
        if (file is not ILastModifiedAtOffset lastModifiedAtOffset)
            return;

        Assert.AreEqual(nameof(ILastModifiedAtOffset.LastModifiedAtOffset), lastModifiedAtOffset.LastModifiedAtOffset.Name);
    }

    [TestMethod]
    public async Task LastModifiedAtOffset_GetValueAsync_ReturnsValue()
    {
        var file = await CreateFileAsync();
        if (file is not ILastModifiedAtOffset lastModifiedAtOffset)
            return;

        var value = await lastModifiedAtOffset.LastModifiedAtOffset.GetValueAsync(CancellationToken.None);

        switch (LastModifiedAtAvailability)
        {
            case PropertyValueAvailability.Always:
                Assert.IsNotNull(value, "LastModifiedAtOffset should always have a value for this implementation.");
                Assert.AreNotEqual(DateTimeOffset.MinValue, value.Value);
                break;

            case PropertyValueAvailability.Maybe:
                if (value is not null)
                    Assert.AreNotEqual(DateTimeOffset.MinValue, value.Value);
                break;
        }
    }

    [TestMethod]
    public async Task LastModifiedAtOffset_GetValueAsync_ImmediateCancellation()
    {
        var file = await CreateFileAsync();
        if (file is not ILastModifiedAtOffset lastModifiedAtOffset)
            return;

        var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            lastModifiedAtOffset.LastModifiedAtOffset.GetValueAsync(cts.Token));
    }

    [TestMethod]
    public async Task LastModifiedAtOffset_Watcher_ReturnsNewInstanceEachCall()
    {
        var file = await CreateFileAsync();
        if (file is not ILastModifiedAtOffset { LastModifiedAtOffset: IMutableStorageProperty<DateTimeOffset?> prop })
            return;

        using var watcher1 = await prop.GetWatcherAsync(CancellationToken.None);
        using var watcher2 = await prop.GetWatcherAsync(CancellationToken.None);

        Assert.AreNotSame(watcher1, watcher2);
    }

    [TestMethod]
    public async Task LastModifiedAtOffset_Watcher_PropertyReferenceIsCorrect()
    {
        var file = await CreateFileAsync();
        if (file is not ILastModifiedAtOffset { LastModifiedAtOffset: IMutableStorageProperty<DateTimeOffset?> prop })
            return;

        using var watcher = await prop.GetWatcherAsync(CancellationToken.None);

        Assert.AreSame(prop, watcher.Property);
    }
}