using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.CommonTests;

/// <summary>
/// Tests for <see cref="IStoragePropertyWatcher{T}"/> implementations.
/// These tests verify that property watchers correctly detect and report property changes.
/// </summary>
public abstract partial class CommonIModifiableFolderTests
{
    /// <summary>
    /// Tests that a mutable property returns a non-null watcher.
    /// </summary>
    [TestMethod]
    public async Task PropertyWatcher_GetWatcherAsync_ReturnsNonNullWatcher()
    {
        var folder = await CreateModifiableFolderWithItems(1, 0);
        var file = await folder.GetFilesAsync().FirstAsync();

        // Find a mutable property to test
        IStoragePropertyWatcher<DateTime?>? watcher = null;

        if (file is ILastModifiedAt { LastModifiedAt: IMutableStorageProperty<DateTime?> mutableProp })
        {
            watcher = await mutableProp.GetWatcherAsync(CancellationToken.None);
        }

        if (watcher is null)
        {
            // No mutable properties available
            return;
        }

        try
        {
            Assert.IsNotNull(watcher, "GetWatcherAsync should return a non-null watcher");
            Assert.IsNotNull(watcher.Property, "Watcher.Property should reference the watched property");
        }
        finally
        {
            watcher.Dispose();
        }
    }

    /// <summary>
    /// Tests that ValueUpdated fires when the timestamp is changed externally (via the property interface).
    /// </summary>
    [TestMethod]
    public async Task PropertyWatcher_ValueUpdated_FiresOnPropertyUpdate()
    {
        var folder = await CreateModifiableFolderWithItems(1, 0);
        var file = await folder.GetFilesAsync().FirstAsync();

        // Need a modifiable + mutable property
        if (file is not ILastModifiedAt { LastModifiedAt: IModifiableStorageProperty<DateTime?> modifiableProp })
            return;

        if (modifiableProp is not IMutableStorageProperty<DateTime?> mutableProp)
            return;

        var watcher = await mutableProp.GetWatcherAsync(CancellationToken.None);
        var eventFired = new TaskCompletionSource<DateTime?>();
        var eventCount = 0;

        watcher.ValueUpdated += (sender, newValue) =>
        {
            eventCount++;
            eventFired.TrySetResult(newValue);
        };

        try
        {
            // Change the property value
            var newTime = DateTime.UtcNow.AddDays(-10);
            await modifiableProp.UpdateValueAsync(newTime, CancellationToken.None);

            // Wait for event or timeout
            var timeoutTask = Task.Delay(3000);
            var completedTask = await Task.WhenAny(eventFired.Task, timeoutTask);

            Assert.IsTrue(eventFired.Task.IsCompleted, "ValueUpdated should fire when property is updated");
            Assert.IsTrue(eventCount >= 1, "ValueUpdated should fire at least once");

            // Verify the value is approximately correct (within 2 seconds tolerance)
            var reportedValue = await eventFired.Task;
            Assert.IsNotNull(reportedValue, "ValueUpdated should provide the new value");
            Assert.IsTrue(Math.Abs((reportedValue.Value.ToUniversalTime() - newTime).TotalSeconds) < 2,
                $"ValueUpdated should report the correct new value. Expected={newTime:O}, Actual={reportedValue:O}");
        }
        finally
        {
            watcher.Dispose();
        }
    }

    /// <summary>
    /// Tests that ValueUpdated fires when the file content is modified (which updates LastModifiedAt).
    /// </summary>
    [TestMethod]
    public async Task PropertyWatcher_ValueUpdated_FiresOnContentChange()
    {
        var folder = await CreateModifiableFolderWithItems(1, 0);
        var file = await folder.GetFilesAsync().FirstAsync();

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
            using (var stream = await file.OpenStreamAsync(FileAccess.Write, CancellationToken.None))
            {
                var data = Encoding.UTF8.GetBytes("Modified content at " + DateTime.UtcNow);
                await stream.WriteAsync(data, 0, data.Length);
            }

            // Wait for event or timeout
            var timeoutTask = Task.Delay(3000);
            var completedTask = await Task.WhenAny(eventFired.Task, timeoutTask);

            Assert.IsTrue(eventFired.Task.IsCompleted, "ValueUpdated should fire when file content changes");

            var reportedValue = await eventFired.Task;
            Assert.IsNotNull(reportedValue, "ValueUpdated should provide the new value");
            Assert.IsTrue(reportedValue > originalValue, "LastModifiedAt should be newer after content change");
        }
        finally
        {
            watcher.Dispose();
        }
    }

    /// <summary>
    /// Tests that disposing the watcher stops events from being raised.
    /// </summary>
    [TestMethod]
    public async Task PropertyWatcher_Dispose_StopsEvents()
    {
        var folder = await CreateModifiableFolderWithItems(1, 0);
        var file = await folder.GetFilesAsync().FirstAsync();

        if (file is not ILastModifiedAt { LastModifiedAt: IModifiableStorageProperty<DateTime?> modifiableProp })
            return;

        if (modifiableProp is not IMutableStorageProperty<DateTime?> mutableProp)
            return;

        var watcher = await mutableProp.GetWatcherAsync(CancellationToken.None);
        var eventCount = 0;

        watcher.ValueUpdated += (sender, newValue) =>
        {
            eventCount++;
        };

        // Dispose the watcher
        watcher.Dispose();

        // Change the property value after disposal
        var newTime = DateTime.UtcNow.AddDays(-5);
        await modifiableProp.UpdateValueAsync(newTime, CancellationToken.None);

        // Wait a bit to ensure no events fire
        await Task.Delay(1000);

        Assert.AreEqual(0, eventCount, "ValueUpdated should not fire after watcher is disposed");
    }

    /// <summary>
    /// Tests that multiple watchers for the same property all receive events.
    /// </summary>
    [TestMethod]
    public async Task PropertyWatcher_MultipleWatchers_AllReceiveEvents()
    {
        var folder = await CreateModifiableFolderWithItems(1, 0);
        var file = await folder.GetFilesAsync().FirstAsync();

        if (file is not ILastModifiedAt { LastModifiedAt: IModifiableStorageProperty<DateTime?> modifiableProp })
            return;

        if (modifiableProp is not IMutableStorageProperty<DateTime?> mutableProp)
            return;

        var watcher1 = await mutableProp.GetWatcherAsync(CancellationToken.None);
        var watcher2 = await mutableProp.GetWatcherAsync(CancellationToken.None);

        var event1Fired = new TaskCompletionSource<bool>();
        var event2Fired = new TaskCompletionSource<bool>();

        watcher1.ValueUpdated += (sender, newValue) => event1Fired.TrySetResult(true);
        watcher2.ValueUpdated += (sender, newValue) => event2Fired.TrySetResult(true);

        try
        {
            // Change the property value
            var newTime = DateTime.UtcNow.AddDays(-10);
            await modifiableProp.UpdateValueAsync(newTime, CancellationToken.None);

            // Wait for both events
            var timeoutTask = Task.Delay(3000);
            await Task.WhenAny(Task.WhenAll(event1Fired.Task, event2Fired.Task), timeoutTask);

            Assert.IsTrue(event1Fired.Task.IsCompleted, "First watcher should receive ValueUpdated");
            Assert.IsTrue(event2Fired.Task.IsCompleted, "Second watcher should receive ValueUpdated");
        }
        finally
        {
            watcher1.Dispose();
            watcher2.Dispose();
        }
    }

    /// <summary>
    /// Tests that multiple separate instances of the same property all receive events.
    /// This forces the test to rely on the underlying storage system (e.g. filesystem events)
    /// rather than local event aggregation on a single object instance.
    /// </summary>
    [TestMethod]
    public async Task PropertyWatcher_CrossInstance_Notification()
    {
        var folder = await CreateModifiableFolderWithItems(1, 0);
        
        // Get the file twice, creating two separate "instances" representing the same resource
        var fileInstance1 = await folder.GetFilesAsync().FirstAsync();
        var fileInstance2 = await folder.GetFilesAsync().FirstAsync();

        if (fileInstance1 is not ILastModifiedAt { LastModifiedAt: IMutableStorageProperty<DateTime?> mutableProp1 })
            return;
            
        if (fileInstance2 is not ILastModifiedAt { LastModifiedAt: IModifiableStorageProperty<DateTime?> modifiableProp2 })
            return;

        // Watch on instance 1
        var watcher1 = await mutableProp1.GetWatcherAsync(CancellationToken.None);
        var eventFired = new TaskCompletionSource<DateTime?>();

        watcher1.ValueUpdated += (sender, newValue) =>
        {
            eventFired.TrySetResult(newValue);
        };

        try
        {
            // Update on instance 2
            var newTime = DateTime.UtcNow.AddDays(-10);
            await modifiableProp2.UpdateValueAsync(newTime, CancellationToken.None);

            // Wait for event on instance 1
            var timeoutTask = Task.Delay(3000);
            await Task.WhenAny(eventFired.Task, timeoutTask);

            Assert.IsTrue(eventFired.Task.IsCompleted, "Watcher on separate instance should receive ValueUpdated");
            
            var reportedValue = await eventFired.Task;
            Assert.IsNotNull(reportedValue, "ValueUpdated should provide the new value");
            Assert.IsTrue(Math.Abs((reportedValue.Value.ToUniversalTime() - newTime).TotalSeconds) < 2,
                $"ValueUpdated should report the correct new value. Expected={newTime:O}, Actual={reportedValue:O}");
        }
        finally
        {
            watcher1.Dispose();
        }
    }

    /// <summary>
    /// Tests that disposing one watcher doesn't affect other watchers.
    /// </summary>
    [TestMethod]
    public async Task PropertyWatcher_DisposeOne_OthersStillWork()
    {
        var folder = await CreateModifiableFolderWithItems(1, 0);
        var file = await folder.GetFilesAsync().FirstAsync();

        if (file is not ILastModifiedAt { LastModifiedAt: IModifiableStorageProperty<DateTime?> modifiableProp })
            return;

        if (modifiableProp is not IMutableStorageProperty<DateTime?> mutableProp)
            return;

        var watcher1 = await mutableProp.GetWatcherAsync(CancellationToken.None);
        var watcher2 = await mutableProp.GetWatcherAsync(CancellationToken.None);

        var event1Count = 0;
        var event2Fired = new TaskCompletionSource<bool>();

        watcher1.ValueUpdated += (sender, newValue) => event1Count++;
        watcher2.ValueUpdated += (sender, newValue) => event2Fired.TrySetResult(true);

        // Dispose only the first watcher
        watcher1.Dispose();

        try
        {
            // Change the property value
            var newTime = DateTime.UtcNow.AddDays(-10);
            await modifiableProp.UpdateValueAsync(newTime, CancellationToken.None);

            // Wait for event
            var timeoutTask = Task.Delay(3000);
            await Task.WhenAny(event2Fired.Task, timeoutTask);

            Assert.AreEqual(0, event1Count, "Disposed watcher should not receive events");
            Assert.IsTrue(event2Fired.Task.IsCompleted, "Non-disposed watcher should still receive events");
        }
        finally
        {
            watcher2.Dispose();
        }
    }

    /// <summary>
    /// Tests async disposal of watcher.
    /// </summary>
    [TestMethod]
    public async Task PropertyWatcher_DisposeAsync_StopsEvents()
    {
        var folder = await CreateModifiableFolderWithItems(1, 0);
        var file = await folder.GetFilesAsync().FirstAsync();

        if (file is not ILastModifiedAt { LastModifiedAt: IModifiableStorageProperty<DateTime?> modifiableProp })
            return;

        if (modifiableProp is not IMutableStorageProperty<DateTime?> mutableProp)
            return;

        var watcher = await mutableProp.GetWatcherAsync(CancellationToken.None);
        var eventCount = 0;

        watcher.ValueUpdated += (sender, newValue) =>
        {
            eventCount++;
        };

        // Dispose asynchronously
        await watcher.DisposeAsync();

        // Change the property value after disposal
        var newTime = DateTime.UtcNow.AddDays(-5);
        await modifiableProp.UpdateValueAsync(newTime, CancellationToken.None);

        // Wait a bit to ensure no events fire
        await Task.Delay(1000);

        Assert.AreEqual(0, eventCount, "ValueUpdated should not fire after watcher is disposed asynchronously");
    }

    /// <summary>
    /// Tests that CreatedAt property watcher works (if supported).
    /// </summary>
    [TestMethod]
    public async Task PropertyWatcher_CreatedAt_ValueUpdated_Fires()
    {
        var folder = await CreateModifiableFolderWithItems(1, 0);
        var file = await folder.GetFilesAsync().FirstAsync();

        if (file is not ICreatedAt { CreatedAt: IModifiableStorageProperty<DateTime?> modifiableProp })
            return;

        if (modifiableProp is not IMutableStorageProperty<DateTime?> mutableProp)
            return;

        var watcher = await mutableProp.GetWatcherAsync(CancellationToken.None);
        var eventFired = new TaskCompletionSource<DateTime?>();

        watcher.ValueUpdated += (sender, newValue) =>
        {
            eventFired.TrySetResult(newValue);
        };

        try
        {
            var newTime = DateTime.UtcNow.AddDays(-30);
            await modifiableProp.UpdateValueAsync(newTime, CancellationToken.None);

            var timeoutTask = Task.Delay(3000);
            await Task.WhenAny(eventFired.Task, timeoutTask);

            Assert.IsTrue(eventFired.Task.IsCompleted, "CreatedAt watcher should fire ValueUpdated on change");
        }
        finally
        {
            watcher.Dispose();
        }
    }

    /// <summary>
    /// Tests that LastAccessedAt property watcher works (if supported).
    /// </summary>
    [TestMethod]
    public async Task PropertyWatcher_LastAccessedAt_ValueUpdated_Fires()
    {
        var folder = await CreateModifiableFolderWithItems(1, 0);
        var file = await folder.GetFilesAsync().FirstAsync();

        if (file is not ILastAccessedAt { LastAccessedAt: IModifiableStorageProperty<DateTime?> modifiableProp })
            return;

        if (modifiableProp is not IMutableStorageProperty<DateTime?> mutableProp)
            return;

        var watcher = await mutableProp.GetWatcherAsync(CancellationToken.None);
        var eventFired = new TaskCompletionSource<DateTime?>();

        watcher.ValueUpdated += (sender, newValue) =>
        {
            eventFired.TrySetResult(newValue);
        };

        try
        {
            var newTime = DateTime.UtcNow.AddDays(-7);
            await modifiableProp.UpdateValueAsync(newTime, CancellationToken.None);

            var timeoutTask = Task.Delay(3000);
            await Task.WhenAny(eventFired.Task, timeoutTask);

            Assert.IsTrue(eventFired.Task.IsCompleted, "LastAccessedAt watcher should fire ValueUpdated on change");
        }
        finally
        {
            watcher.Dispose();
        }
    }
}