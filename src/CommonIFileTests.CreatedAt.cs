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

        switch (CreatedAtAvailability)
        {
            case PropertyValueAvailability.Always:
                Assert.IsNotNull(value, "CreatedAt should always have a value for this implementation.");
                Assert.AreNotEqual(DateTime.MinValue, value.Value);
                break;

            case PropertyValueAvailability.Maybe:
                if (value is not null)
                    Assert.AreNotEqual(DateTime.MinValue, value.Value);
                break;
        }
    }

    [TestMethod]
    public async Task CreatedAt_CreateWithKnownTimestamp_ReturnsCorrectValue()
    {
        var expectedTimestamp = DateTime.UtcNow.AddDays(-30);
        var file = await CreateFileWithCreatedAtAsync(expectedTimestamp);
        
        // Skip if implementation doesn't support creating files with known timestamps
        if (file is null)
            return;

        if (file is not ICreatedAt createdAt)
        {
            Assert.Fail("CreateFileWithCreatedAtAsync returned a file that doesn't implement ICreatedAt.");
            return;
        }

        var value = await createdAt.CreatedAt.GetValueAsync(CancellationToken.None);
        
        Assert.IsNotNull(value, "CreatedAt should have a value when created with a known timestamp.");
        
        var diff = Math.Abs((value.Value.ToUniversalTime() - expectedTimestamp).TotalSeconds);
        Assert.IsTrue(diff < 2, $"CreatedAt should match the requested timestamp. Expected={expectedTimestamp:O}, Actual={value:O}, Diff={diff:F2}s");
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
}
