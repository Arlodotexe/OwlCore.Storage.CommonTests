using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OwlCore.Storage.CommonTests;

public abstract partial class CommonIFolderTests
{
    [TestMethod]
    public async Task LastAccessedAtOffset_PropertyName_MatchesInterfaceMember()
    {
        var folder = await CreateFolderAsync();
        if (folder is not ILastAccessedAtOffset lastAccessedAtOffset)
            return;

        Assert.AreEqual(nameof(ILastAccessedAtOffset.LastAccessedAtOffset), lastAccessedAtOffset.LastAccessedAtOffset.Name);
    }

    [TestMethod]
    public async Task LastAccessedAtOffset_GetValueAsync_ReturnsValue()
    {
        var folder = await CreateFolderAsync();
        if (folder is not ILastAccessedAtOffset lastAccessedAtOffset)
            return;

        var value = await lastAccessedAtOffset.LastAccessedAtOffset.GetValueAsync(CancellationToken.None);

        switch (LastAccessedAtAvailability)
        {
            case PropertyValueAvailability.Always:
                Assert.IsNotNull(value, "LastAccessedAtOffset should always have a value for this implementation.");
                Assert.AreNotEqual(DateTimeOffset.MinValue, value.Value);
                break;

            case PropertyValueAvailability.Maybe:
                if (value is not null)
                    Assert.AreNotEqual(DateTimeOffset.MinValue, value.Value);
                break;
        }
    }

    [TestMethod]
    public async Task LastAccessedAtOffset_GetValueAsync_ImmediateCancellation()
    {
        var folder = await CreateFolderAsync();
        if (folder is not ILastAccessedAtOffset lastAccessedAtOffset)
            return;

        var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            lastAccessedAtOffset.LastAccessedAtOffset.GetValueAsync(cts.Token));
    }

    [TestMethod]
    public async Task LastAccessedAtOffset_Watcher_ReturnsNewInstanceEachCall()
    {
        var folder = await CreateFolderAsync();
        if (folder is not ILastAccessedAtOffset { LastAccessedAtOffset: IMutableStorageProperty<DateTimeOffset?> prop })
            return;

        using var watcher1 = await prop.GetWatcherAsync(CancellationToken.None);
        using var watcher2 = await prop.GetWatcherAsync(CancellationToken.None);

        Assert.AreNotSame(watcher1, watcher2);
    }

    [TestMethod]
    public async Task LastAccessedAtOffset_Watcher_PropertyReferenceIsCorrect()
    {
        var folder = await CreateFolderAsync();
        if (folder is not ILastAccessedAtOffset { LastAccessedAtOffset: IMutableStorageProperty<DateTimeOffset?> prop })
            return;

        using var watcher = await prop.GetWatcherAsync(CancellationToken.None);

        Assert.AreSame(prop, watcher.Property);
    }
}