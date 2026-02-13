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

        switch (CreatedAtAvailability)
        {
            case PropertyValueAvailability.Always:
                Assert.IsNotNull(value, "CreatedAtOffset should always have a value for this implementation.");
                Assert.AreNotEqual(DateTimeOffset.MinValue, value.Value);
                break;

            case PropertyValueAvailability.Maybe:
                if (value is not null)
                    Assert.AreNotEqual(DateTimeOffset.MinValue, value.Value);
                break;
        }
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
}