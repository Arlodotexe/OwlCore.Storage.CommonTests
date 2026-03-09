using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OwlCore.Storage.CommonTests;

public abstract partial class CommonIFolderTests
{
    [TestMethod]
    public async Task LastAccessedAt_PropertyName_MatchesInterfaceMember()
    {
        var folder = await CreateFolderAsync();
        if (folder is not ILastAccessedAt lastAccessedAt)
            return;

        Assert.AreEqual(nameof(ILastAccessedAt.LastAccessedAt), lastAccessedAt.LastAccessedAt.Name);
    }

    [TestMethod]
    public async Task LastAccessedAt_GetValueAsync_ReturnsValue()
    {
        var folder = await CreateFolderAsync();
        if (folder is not ILastAccessedAt lastAccessedAt)
            return;

        var value = await lastAccessedAt.LastAccessedAt.GetValueAsync(CancellationToken.None);

        switch (LastAccessedAtAvailability)
        {
            case PropertyValueAvailability.Always:
                Assert.IsNotNull(value, "LastAccessedAt should always have a value for this implementation.");
                Assert.AreNotEqual(DateTime.MinValue, value.Value);
                break;

            case PropertyValueAvailability.Maybe:
                if (value is not null)
                    Assert.AreNotEqual(DateTime.MinValue, value.Value);
                break;
        }
    }

    [TestMethod]
    public async Task LastAccessedAt_CreateWithKnownTimestamp_ReturnsCorrectValue()
    {
        var expectedTimestamp = DateTime.UtcNow.AddDays(-7);
        var folder = await CreateFolderWithLastAccessedAtAsync(expectedTimestamp);
        
        // Skip if implementation doesn't support creating folders with known timestamps
        if (folder is null)
            return;

        if (folder is not ILastAccessedAt lastAccessedAt)
        {
            Assert.Fail("CreateFolderWithLastAccessedAtAsync returned a folder that doesn't implement ILastAccessedAt.");
            return;
        }

        var value = await lastAccessedAt.LastAccessedAt.GetValueAsync(CancellationToken.None);
        
        Assert.IsNotNull(value, "LastAccessedAt should have a value when created with a known timestamp.");
        
        var diff = Math.Abs((value.Value.ToUniversalTime() - expectedTimestamp).TotalSeconds);
        Assert.IsTrue(diff < 2, $"LastAccessedAt should match the requested timestamp. Expected={expectedTimestamp:O}, Actual={value:O}, Diff={diff:F2}s");
    }

    [TestMethod]
    public async Task LastAccessedAt_GetValueAsync_ImmediateCancellation()
    {
        var folder = await CreateFolderAsync();
        if (folder is not ILastAccessedAt lastAccessedAt)
            return;

        var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            lastAccessedAt.LastAccessedAt.GetValueAsync(cts.Token));
    }

    [TestMethod]
    public async Task LastAccessedAt_Watcher_ReturnsNewInstanceEachCall()
    {
        var folder = await CreateFolderAsync();
        if (folder is not ILastAccessedAt { LastAccessedAt: IMutableStorageProperty<DateTime?> prop })
            return;

        using var watcher1 = await prop.GetWatcherAsync(CancellationToken.None);
        using var watcher2 = await prop.GetWatcherAsync(CancellationToken.None);

        Assert.AreNotSame(watcher1, watcher2);
    }

    [TestMethod]
    public async Task LastAccessedAt_Watcher_PropertyReferenceIsCorrect()
    {
        var folder = await CreateFolderAsync();
        if (folder is not ILastAccessedAt { LastAccessedAt: IMutableStorageProperty<DateTime?> prop })
            return;

        using var watcher = await prop.GetWatcherAsync(CancellationToken.None);

        Assert.AreSame(prop, watcher.Property);
    }
}
