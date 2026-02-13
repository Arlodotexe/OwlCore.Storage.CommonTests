using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OwlCore.Storage.CommonTests;

public abstract partial class CommonIFileTests
{
    [TestMethod]
    public async Task LastAccessedAt_PropertyName_MatchesInterfaceMember()
    {
        var file = await CreateFileAsync();
        if (file is not ILastAccessedAt lastAccessedAt)
            return;

        Assert.AreEqual(nameof(ILastAccessedAt.LastAccessedAt), lastAccessedAt.LastAccessedAt.Name);
    }

    [TestMethod]
    public async Task LastAccessedAt_GetValueAsync_ReturnsValue()
    {
        var file = await CreateFileAsync();
        if (file is not ILastAccessedAt lastAccessedAt)
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
        var file = await CreateFileWithLastAccessedAtAsync(expectedTimestamp);
        
        // Skip if implementation doesn't support creating files with known timestamps
        if (file is null)
            return;

        if (file is not ILastAccessedAt lastAccessedAt)
        {
            Assert.Fail("CreateFileWithLastAccessedAtAsync returned a file that doesn't implement ILastAccessedAt.");
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
        var file = await CreateFileAsync();
        if (file is not ILastAccessedAt lastAccessedAt)
            return;

        var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsException<OperationCanceledException>(() =>
            lastAccessedAt.LastAccessedAt.GetValueAsync(cts.Token));
    }

    [TestMethod]
    public async Task LastAccessedAt_Watcher_ReturnsNewInstanceEachCall()
    {
        var file = await CreateFileAsync();
        if (file is not ILastAccessedAt { LastAccessedAt: IMutableStorageProperty<DateTime?> prop })
            return;

        using var watcher1 = await prop.GetWatcherAsync(CancellationToken.None);
        using var watcher2 = await prop.GetWatcherAsync(CancellationToken.None);

        Assert.AreNotSame(watcher1, watcher2);
    }

    [TestMethod]
    public async Task LastAccessedAt_Watcher_PropertyReferenceIsCorrect()
    {
        var file = await CreateFileAsync();
        if (file is not ILastAccessedAt { LastAccessedAt: IMutableStorageProperty<DateTime?> prop })
            return;

        using var watcher = await prop.GetWatcherAsync(CancellationToken.None);

        Assert.AreSame(prop, watcher.Property);
    }
}
