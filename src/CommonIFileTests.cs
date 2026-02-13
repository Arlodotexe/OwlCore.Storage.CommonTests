using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OwlCore.Storage.CommonTests;

public abstract partial class CommonIFileTests
{
    /// <summary>
    /// Gets a boolean that indicates if the file support writing to the underlying stream.
    /// </summary>
    public virtual bool SupportsWriting => true;

    /// <summary>
    /// Gets the expected availability of the CreatedAt property value.
    /// </summary>
    public virtual PropertyValueAvailability CreatedAtAvailability => PropertyValueAvailability.Always;

    /// <summary>
    /// Gets the expected availability of the LastModifiedAt property value.
    /// </summary>
    public virtual PropertyValueAvailability LastModifiedAtAvailability => PropertyValueAvailability.Always;

    /// <summary>
    /// Gets the expected availability of the LastAccessedAt property value.
    /// </summary>
    public virtual PropertyValueAvailability LastAccessedAtAvailability => PropertyValueAvailability.Always;

    /// <summary>
    /// Gets the expected update behavior of the LastModifiedAt property.
    /// </summary>
    public virtual PropertyUpdateBehavior LastModifiedAtUpdateBehavior => PropertyUpdateBehavior.Immediate;

    /// <summary>
    /// Gets the expected update behavior of the LastAccessedAt property.
    /// </summary>
    public virtual PropertyUpdateBehavior LastAccessedAtUpdateBehavior => PropertyUpdateBehavior.Immediate;

    /// <summary>
    /// Gets the expected update behavior of the CreatedAt property.
    /// </summary>
    public virtual PropertyUpdateBehavior CreatedAtUpdateBehavior => PropertyUpdateBehavior.Immediate;

    /// <summary>
    /// Call the constructor using valid input parameters.
    /// </summary>
    public abstract Task<IFile> CreateFileAsync();

    /// <summary>
    /// Creates a file with a specific CreatedAt timestamp.
    /// </summary>
    /// <param name="createdAt">The creation timestamp to apply.</param>
    /// <returns>The created file, or null if the implementation doesn't support setting CreatedAt.</returns>
    /// <remarks>
    /// Implementations must explicitly return null if they don't support setting CreatedAt,
    /// which signals the test should be skipped. This ensures gaps are surfaced rather than silently ignored.
    /// </remarks>
    public abstract Task<IFile?> CreateFileWithCreatedAtAsync(DateTime createdAt);

    /// <summary>
    /// Creates a file with a specific LastModifiedAt timestamp.
    /// </summary>
    /// <param name="lastModifiedAt">The last modified timestamp to apply.</param>
    /// <returns>The created file, or null if the implementation doesn't support setting LastModifiedAt.</returns>
    /// <remarks>
    /// Implementations must explicitly return null if they don't support setting LastModifiedAt,
    /// which signals the test should be skipped. This ensures gaps are surfaced rather than silently ignored.
    /// </remarks>
    public abstract Task<IFile?> CreateFileWithLastModifiedAtAsync(DateTime lastModifiedAt);

    /// <summary>
    /// Creates a file with a specific LastAccessedAt timestamp.
    /// </summary>
    /// <param name="lastAccessedAt">The last accessed timestamp to apply.</param>
    /// <returns>The created file, or null if the implementation doesn't support setting LastAccessedAt.</returns>
    /// <remarks>
    /// Implementations must explicitly return null if they don't support setting LastAccessedAt,
    /// which signals the test should be skipped. This ensures gaps are surfaced rather than silently ignored.
    /// </remarks>
    public abstract Task<IFile?> CreateFileWithLastAccessedAtAsync(DateTime lastAccessedAt);

    [TestMethod]
    public Task ConstructorCall_ValidParameters()
    {
        // Shouldn't throw when constructor is called.
        return CreateFileAsync();
    }

    [TestMethod]
    public async Task IdNotNullOrWhiteSpace()
    {
        var file = await CreateFileAsync();

        Assert.IsFalse(string.IsNullOrWhiteSpace(file.Id));
    }

    [TestMethod]
    [AllEnumFlagCombinations(typeof(FileAccess))]
    public async Task OpenStreamAndTryEachAccessMode(FileAccess accessMode)
    {
        var file = await CreateFileAsync();

        if (accessMode == 0)
        {
            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => file.OpenStreamAsync(accessMode));
            return;
        }

        // Don't test writing if not supported - remove the Write flag using AND NOT.
        if (!SupportsWriting)
            accessMode &= ~FileAccess.Write;

        // If removing write access resulted in empty flag.
        if (accessMode == 0)
            return;

        using var stream = await file.OpenStreamAsync(accessMode);

        if (accessMode.HasFlag(FileAccess.Read))
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            Assert.AreNotEqual(0, memoryStream.ToArray().Length);
        }

        if (accessMode.HasFlag(FileAccess.Write) && SupportsWriting)
        {
            stream.WriteByte(0);
        }
    }

    [TestMethod]
    [AllEnumFlagCombinations(typeof(FileAccess))]
    public async Task OpenStreamWithEachAccessModeAndCancelToken(FileAccess accessMode)
    {
        var cancellationTokenSource = new CancellationTokenSource();

        var file = await CreateFileAsync();

        if (accessMode == 0)
        {
            var task = Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => file.OpenStreamAsync(accessMode, cancellationTokenSource.Token));
            cancellationTokenSource.Cancel();

            await task;
            return;
        }

        cancellationTokenSource.Cancel();

        await AssertEx.ThrowsExceptionAsync<OperationCanceledException>(async () =>
        {
            try
            {
                await file.OpenStreamAsync(accessMode, cancellationTokenSource.Token);
            }
            catch (TaskCanceledException e)
            {
                throw new OperationCanceledException(e.Message);
            }
        });
    }
}
