using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OwlCore.Storage.CommonTests;

public abstract partial class CommonIFolderTests
{
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
    /// Gets whether the implementation allows <see cref="IStorable.Id"/> to equal <see cref="IStorable.Name"/>.
    /// </summary>
    /// <remarks>
    /// Most implementations should keep this <c>false</c> to ensure IDs are unique and not derived from names.
    /// Content-addressed storage (e.g., IPFS) may set this to <c>true</c> since the CID serves as both a unique identifier and a valid name.
    /// </remarks>
    public virtual bool AllowsIdEqualToName => false;

    /// <summary>
    /// Call the constructor using valid input parameters.
    /// </summary>
    public abstract Task<IFolder> CreateFolderAsync();

    /// <summary>
    /// Creates a folder with items in it.
    /// </summary>
    public abstract Task<IFolder> CreateFolderWithItems(int fileCount, int folderCount);

    /// <summary>
    /// Creates a folder with a specific CreatedAt timestamp.
    /// </summary>
    /// <param name="createdAt">The creation timestamp to apply.</param>
    /// <returns>The created folder, or null if the implementation doesn't support setting CreatedAt.</returns>
    /// <remarks>
    /// Implementations must explicitly return null if they don't support setting CreatedAt,
    /// which signals the test should be skipped. This ensures gaps are surfaced rather than silently ignored.
    /// </remarks>
    public abstract Task<IFolder?> CreateFolderWithCreatedAtAsync(DateTime createdAt);

    /// <summary>
    /// Creates a folder with a specific LastModifiedAt timestamp.
    /// </summary>
    /// <param name="lastModifiedAt">The last modified timestamp to apply.</param>
    /// <returns>The created folder, or null if the implementation doesn't support setting LastModifiedAt.</returns>
    /// <remarks>
    /// Implementations must explicitly return null if they don't support setting LastModifiedAt,
    /// which signals the test should be skipped. This ensures gaps are surfaced rather than silently ignored.
    /// </remarks>
    public abstract Task<IFolder?> CreateFolderWithLastModifiedAtAsync(DateTime lastModifiedAt);

    /// <summary>
    /// Creates a folder with a specific LastAccessedAt timestamp.
    /// </summary>
    /// <param name="lastAccessedAt">The last accessed timestamp to apply.</param>
    /// <returns>The created folder, or null if the implementation doesn't support setting LastAccessedAt.</returns>
    /// <remarks>
    /// Implementations must explicitly return null if they don't support setting LastAccessedAt,
    /// which signals the test should be skipped. This ensures gaps are surfaced rather than silently ignored.
    /// </remarks>
    public abstract Task<IFolder?> CreateFolderWithLastAccessedAtAsync(DateTime lastAccessedAt);

    [TestMethod]
    public Task ConstructorCall_ValidParameters()
    {
        // Shouldn't throw when constructor is called.
        return CreateFolderAsync();
    }

    [TestMethod]
    public async Task HasValidName()
    {
        var folder = await CreateFolderAsync();

        Assert.IsFalse(string.IsNullOrWhiteSpace(folder.Name));
    }

    [TestMethod]
    public async Task HasValidId()
    {
        var folder = await CreateFolderAsync();

        Assert.IsFalse(string.IsNullOrWhiteSpace(folder.Id));

        if (!AllowsIdEqualToName)
            Assert.AreNotEqual(folder.Name, folder.Id, "Names should not be used as a unique identifier. Use something more specific.");
    }

    [TestMethod]
    [DataRow(StorableType.None, 0, 0)]
    [DataRow(StorableType.None, 2, 2)]

    [DataRow(StorableType.File, 2, 0),
     DataRow(StorableType.File, 0, 2),
     DataRow(StorableType.File, 0, 0)]

    [DataRow(StorableType.Folder, 2, 0),
     DataRow(StorableType.Folder, 0, 2),
     DataRow(StorableType.Folder, 0, 0)]

    [DataRow(StorableType.Folder | StorableType.File, 2, 0),
     DataRow(StorableType.Folder | StorableType.File, 0, 2),
     DataRow(StorableType.Folder | StorableType.File, 0, 0)]

    [DataRow(StorableType.All, 2, 0),
     DataRow(StorableType.All, 0, 2),
     DataRow(StorableType.All, 0, 0)]
    public async Task GetItemsAsync_AllCombinations(StorableType type, int fileCount, int folderCount)
    {
        var file = await CreateFolderWithItems(fileCount, folderCount);

        if (type == StorableType.None)
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await foreach (var _ in file.GetItemsAsync(type)) { }
            });
            return;
        }

        var returnedFileCount = 0;
        var returnedFolderCount = 0;
        var otherReturnedItemCount = 0;

        await foreach (var item in file.GetItemsAsync(type))
        {
            if (item is IFile)
                returnedFileCount++;
            else if (item is IFolder)
                returnedFolderCount++;
            else
                otherReturnedItemCount++;
        }

        if (type.HasFlag(StorableType.File))
            Assert.AreEqual(fileCount, returnedFileCount, "Incorrect number of files were returned.");

        if (type.HasFlag(StorableType.Folder))
            Assert.AreEqual(folderCount, returnedFolderCount, "Incorrect number of folders were returned.");

        Assert.AreEqual(0, otherReturnedItemCount, "Unknown object types were returned.");
    }

    [TestMethod]
    [AllEnumFlagCombinations(typeof(StorableType))]
    public async Task GetItemsAsync_AllCombinations_ImmediateTokenCancellation(StorableType type)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var folder = await CreateFolderAsync();

        await AssertEx.ThrowsAsync<OperationCanceledException>(async () => await folder.GetItemsAsync(type, cancellationTokenSource.Token).ToListAsync(cancellationToken: cancellationTokenSource.Token), "Does not cancel immediately if a canceled token is passed.");
    }

    [TestMethod]
    [DataRow(StorableType.None, 0, 0)]
    [DataRow(StorableType.None, 2, 2)]

    [DataRow(StorableType.File, 2, 0)]

    [DataRow(StorableType.Folder, 0, 2)]

    [DataRow(StorableType.Folder | StorableType.File, 2, 0),
     DataRow(StorableType.Folder | StorableType.File, 0, 2)]

    [DataRow(StorableType.All, 2, 0),
     DataRow(StorableType.All, 0, 2)]
    public async Task GetItemsAsync_AllCombinations_TokenCancellationDuringEnumeration(StorableType type, int fileCount, int folderCount)
    {
        // No enumeration should take place if set to "None". Tests for this covered elsewhere.
        if (type == StorableType.None)
            return;

        var cancellationTokenSource = new CancellationTokenSource();
        var folder = await CreateFolderWithItems(fileCount, folderCount);

        await AssertEx.ThrowsAsync<OperationCanceledException>(async () =>
        {
            var index = 0;
            await foreach (var item in folder.GetItemsAsync(type, cancellationTokenSource.Token))
            {
                Assert.IsNotNull(item);

                index++;
                if (index > fileCount || index > folderCount)
                {
                    cancellationTokenSource.Cancel();
                }
            }
        });
    }
}
