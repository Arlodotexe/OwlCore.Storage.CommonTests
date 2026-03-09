using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OwlCore.Storage.CommonTests;

public abstract partial class CommonIFileTests
{
    [TestMethod]
    public async Task LastModifiedAt_PropertyName_MatchesInterfaceMember()
    {
        var file = await CreateFileAsync();
        if (file is not ILastModifiedAt lastModifiedAt)
            return;

        Assert.AreEqual(nameof(ILastModifiedAt.LastModifiedAt), lastModifiedAt.LastModifiedAt.Name);
    }

    [TestMethod]
    public async Task LastModifiedAt_GetValueAsync_ReturnsValue()
    {
        var file = await CreateFileAsync();
        if (file is not ILastModifiedAt lastModifiedAt)
            return;

        var value = await lastModifiedAt.LastModifiedAt.GetValueAsync(CancellationToken.None);

        switch (LastModifiedAtAvailability)
        {
            case PropertyValueAvailability.Always:
                Assert.IsNotNull(value, "LastModifiedAt should always have a value for this implementation.");
                Assert.AreNotEqual(DateTime.MinValue, value.Value);
                break;

            case PropertyValueAvailability.Maybe:
                if (value is not null)
                    Assert.AreNotEqual(DateTime.MinValue, value.Value);
                break;
        }
    }

    [TestMethod]
    public async Task LastModifiedAt_CreateWithKnownTimestamp_ReturnsCorrectValue()
    {
        var expectedTimestamp = DateTime.UtcNow.AddDays(-15);
        var file = await CreateFileWithLastModifiedAtAsync(expectedTimestamp);
        
        // Skip if implementation doesn't support creating files with known timestamps
        if (file is null)
            return;

        if (file is not ILastModifiedAt lastModifiedAt)
        {
            Assert.Fail("CreateFileWithLastModifiedAtAsync returned a file that doesn't implement ILastModifiedAt.");
            return;
        }

        var value = await lastModifiedAt.LastModifiedAt.GetValueAsync(CancellationToken.None);
        
        Assert.IsNotNull(value, "LastModifiedAt should have a value when created with a known timestamp.");
        
        var diff = Math.Abs((value.Value.ToUniversalTime() - expectedTimestamp).TotalSeconds);
        Assert.IsTrue(diff < 2, $"LastModifiedAt should match the requested timestamp. Expected={expectedTimestamp:O}, Actual={value:O}, Diff={diff:F2}s");
    }

    [TestMethod]
    public async Task LastModifiedAt_GetValueAsync_ImmediateCancellation()
    {
        var file = await CreateFileAsync();
        if (file is not ILastModifiedAt lastModifiedAt)
            return;

        var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            lastModifiedAt.LastModifiedAt.GetValueAsync(cts.Token));
    }

    [TestMethod]
    public async Task LastModifiedAt_Watcher_ReturnsNewInstanceEachCall()
    {
        var file = await CreateFileAsync();
        if (file is not ILastModifiedAt { LastModifiedAt: IMutableStorageProperty<DateTime?> prop })
            return;

        using var watcher1 = await prop.GetWatcherAsync(CancellationToken.None);
        using var watcher2 = await prop.GetWatcherAsync(CancellationToken.None);

        Assert.AreNotSame(watcher1, watcher2);
    }

    [TestMethod]
    public async Task LastModifiedAt_Watcher_PropertyReferenceIsCorrect()
    {
        var file = await CreateFileAsync();
        if (file is not ILastModifiedAt { LastModifiedAt: IMutableStorageProperty<DateTime?> prop })
            return;

        using var watcher = await prop.GetWatcherAsync(CancellationToken.None);

        Assert.AreSame(prop, watcher.Property);
    }

    

    /// <summary>
    /// Tests that ValueUpdated fires when the file content is modified (which updates LastModifiedAt).
    /// </summary>
    [TestMethod]
    public async Task PropertyWatcher_ValueUpdated_FiresOnContentChange()
    {
        if (LastModifiedAtUpdateBehavior == PropertyUpdateBehavior.Never || !SupportsWriting)
            return;

        var file = await CreateFileAsync();

        if (file is not ILastModifiedAt { LastModifiedAt: IMutableStorageProperty<DateTime?> mutableProp })
            return;

        var watcher = await mutableProp.GetWatcherAsync(CancellationToken.None);
        var eventFired = new TaskCompletionSource<DateTime?>();

        watcher.ValueUpdated += (sender, newValue) =>
        {
            eventFired.TrySetResult(newValue);
        };

        try
        {
            // Capture original value
            var originalValue = await mutableProp.GetValueAsync(CancellationToken.None);

            // Modify file content (should update LastModifiedAt)
            var stream = await file.OpenStreamAsync(FileAccess.Write, CancellationToken.None);

            var data = Encoding.UTF8.GetBytes("Modified content at " + DateTime.UtcNow);

            await stream.WriteAsync(data, 0, data.Length);

            stream.Dispose();

            // Wait for event or timeout
            var timeoutTask = Task.Delay(PropertyWatcherTimeoutMs);
            var completedTask = await Task.WhenAny(eventFired.Task, timeoutTask);

            Assert.IsTrue(eventFired.Task.IsCompleted, "ValueUpdated should fire when file content changes");

            var reportedValue = await eventFired.Task;
            Assert.IsNotNull(reportedValue, "ValueUpdated should provide the new value");

            
        switch (LastModifiedAtUpdateBehavior)
        {
            case PropertyUpdateBehavior.Immediate:
                Assert.IsTrue(reportedValue > originalValue, $"LastModifiedAt should be newer after content change, ID {file.Id}");
                break;

            case PropertyUpdateBehavior.Eventual:
                // For eventual updates, we can't strictly assert
                break;
        }
        }
        finally
        {
            watcher.Dispose();
        }
    }
}
