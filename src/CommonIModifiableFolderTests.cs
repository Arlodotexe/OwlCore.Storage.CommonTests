using Microsoft.VisualStudio.TestTools.UnitTesting;
using OwlCore.Extensions;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace OwlCore.Storage.CommonTests;

public abstract class IModifiableFolderTests : CommonIFolderTests
{
    /// <summary>
    /// Creates a folder with items in it.
    /// </summary>
    public abstract Task<IModifiableFolder> CreateModifiableFolderWithItems(int fileCount, int folderCount);

    /// <summary>
    /// Call the constructor using valid input parameters.
    /// </summary>
    public override async Task<IFolder> CreateFolderAsync() => await CreateModifiableFolderAsync();

    /// <summary>
    /// Call the constructor using valid input parameters.
    /// </summary>
    public abstract Task<IModifiableFolder> CreateModifiableFolderAsync();

    /// <summary>
    /// Creates a folder with items in it.
    /// </summary>
    public override async Task<IFolder> CreateFolderWithItems(int fileCount, int folderCount) => await CreateModifiableFolderWithItems(fileCount, folderCount);

    [TestMethod]
    public async Task DeleteAsyncTest()
    {
        var folder = await CreateModifiableFolderWithItems(1, 1);

        var firstItem = await folder.GetItemsAsync().FirstAsync();

        await folder.DeleteAsync(firstItem);

        var newFirstItem = await folder.GetItemsAsync().FirstAsync();

        Assert.IsFalse(ReferenceEquals(firstItem, newFirstItem));
    }

    [TestMethod]
    public async Task DeleteAsyncSubfolderWithItems()
    {
        var folder = await CreateModifiableFolderWithItems(1, 2);
        var subFolder = await folder.GetFoldersAsync().FirstAsync();

        await ((IModifiableFolder)subFolder).CreateFileAsync("TestFile");
        await folder.DeleteAsync(subFolder);
    }

    [TestMethod]
    public async Task CreateCopyOfAsyncTest()
    {
        var sourceFolder = await CreateModifiableFolderWithItems(1, 0);
        var destinationFolder = await CreateModifiableFolderWithItems(1, 0);

        // Copy random bytes to original file
        var originalFile = await sourceFolder.GetFilesAsync().FirstAsync();
        var randomBytesAddedToOriginalFile = await CopyRandomBytesToAsync(originalFile);

        // Create copy
        var copy = await destinationFolder.CreateCopyOfAsync(originalFile, overwrite: true);

        // Read copy
        using var copyStream = await copy.OpenStreamAsync();
        var copiedBytes = await copyStream.ToBytesAsync();
        CollectionAssert.AreEqual(randomBytesAddedToOriginalFile, copiedBytes);

        // Change the contents of the copy
        // Allows us to check if the contents were successfully copied to another place.
        copyStream.Dispose();
        var copyNewBytes = await CopyRandomBytesToAsync(copy);

        // If the file existed before we copied and we chose not to overwrite it, the "copy" operation turns into an "open" operation.
        var noOverwriteCopy = await sourceFolder.CreateCopyOfAsync(copy, overwrite: false);
        using var noOverwriteCopyStream = await noOverwriteCopy.OpenStreamAsync();
        var noOverwriteCopyBytes = await noOverwriteCopyStream.ToBytesAsync();

        // Make sure the new bytes weren't copied.
        CollectionAssert.AreNotEqual(copyNewBytes, noOverwriteCopyBytes);
    }

    [TestMethod]
    public async Task MoveFromAsyncTest()
    {

    }

    [DataRow(0, 0)]
    [DataRow(1, 1)]
    [DataRow(0, 1)]
    [DataRow(1, 1)]
    [TestMethod]
    public async Task CreateNewFolderAsyncTest_FolderWithItems_NameNotExists(int fileCount, int folderCount)
    {
        var sourceFolder = await CreateModifiableFolderWithItems(fileCount, folderCount);
        await sourceFolder.CreateFolderAsync("name");

        Assert.AreEqual(folderCount + 1, await sourceFolder.GetFoldersAsync().CountAsync());
    }

    [DataRow(0, 0)]
    [DataRow(1, 1)]
    [DataRow(0, 1)]
    [DataRow(1, 1)]
    [TestMethod]
    public async Task CreateNewFolderAsyncTest_FolderWithItems_NameExists_NoOverwrite(int fileCount, int folderCount)
    {
        var sourceFolder = await CreateModifiableFolderWithItems(fileCount, folderCount);

        // If the folder existed beforehand and we chose not to overwrite it, the "create" operation turns into an "open" operation.
        // Create an original item to check against created items, to determine if an overwrite happened.
        var originalFile = await sourceFolder.CreateFolderAsync("name");

        // Create unique content, to check if overwrite happens
        var uniqueContent = await ((IModifiableFolder)originalFile).CreateFolderAsync($"{Guid.NewGuid()}");

        var createdFolder = await sourceFolder.CreateFolderAsync(originalFile.Name, overwrite: false);
        var folderContents = await createdFolder.GetFoldersAsync().ToListAsync();

        Assert.AreEqual(1, folderContents.Count);

        // Make sure the unique content still exists in the folder.
        Assert.IsTrue(folderContents.Any(x => x.Id == uniqueContent.Id));
    }

    [DataRow(0, 0)]
    [DataRow(1, 1)]
    [DataRow(0, 1)]
    [DataRow(1, 1)]
    [TestMethod]
    public async Task CreateNewFolderAsyncTest_FolderWithItems_NameExists_Overwrite(int fileCount, int folderCount)
    {
        var sourceFolder = await CreateModifiableFolderWithItems(fileCount, folderCount);

        // If the item existed beforehand and we chose not to overwrite it, the "create" operation turns into an "open" operation.
        // Create an original item to check against created items, to determine if an overwrite happened.
        var originalFile = await sourceFolder.CreateFolderAsync("name");

        // Create unique content, to check if overwrite happens
        var uniqueContent = await ((IModifiableFolder)originalFile).CreateFolderAsync($"{Guid.NewGuid()}");

        var createdFolder = await sourceFolder.CreateFolderAsync(originalFile.Name, overwrite: true);
        var folderContents = await createdFolder.GetFoldersAsync().ToListAsync();

        Assert.AreEqual(0, folderContents.Count);

        // Make sure the unique content no longer exists in the folder.
        Assert.IsTrue(folderContents.All(x => x.Name != uniqueContent.Name));
    }

    [DataRow(0, 0)]
    [DataRow(1, 1)]
    [DataRow(0, 1)]
    [DataRow(1, 1)]
    [TestMethod]
    public async Task CreateNewFileAsyncTest_FolderWithItems_NameNotExists(int fileCount, int folderCount)
    {
        var sourceFolder = await CreateModifiableFolderWithItems(fileCount, folderCount);
        await sourceFolder.CreateFileAsync("name");

        Assert.AreEqual(fileCount + 1, await sourceFolder.GetFilesAsync().CountAsync());
    }

    [DataRow(0, 0)]
    [DataRow(1, 1)]
    [DataRow(0, 1)]
    [DataRow(1, 1)]
    [TestMethod]
    public async Task CreateNewFileAsyncTest_FolderWithItems_NameExists_NoOverwrite(int fileCount, int folderCount)
    {
        var sourceFolder = await CreateModifiableFolderWithItems(fileCount, folderCount);

        // If the item existed beforehand and we chose not to overwrite it, the "create" operation turns into an "open" operation.
        // Create an original item to check against created items, to determine if an overwrite happened.
        var originalFile = await sourceFolder.CreateFileAsync("name");

        // Create unique content, to check if overwrite happens
        var uniqueContent = await CopyRandomBytesToAsync(originalFile);

        var createdFile = await sourceFolder.CreateFileAsync(originalFile.Name, overwrite: false);
        using var createdFileStream = await createdFile.OpenStreamAsync();
        var createdFileBytes = await createdFileStream.ToBytesAsync();

        // Make sure the unique content still exists.
        CollectionAssert.AreEqual(createdFileBytes, uniqueContent);
    }

    [DataRow(0, 0)]
    [DataRow(1, 1)]
    [DataRow(0, 1)]
    [DataRow(1, 1)]
    [TestMethod]
    public async Task CreateNewFileAsyncTest_FolderWithItems_NameExists_Overwrite(int fileCount, int folderCount)
    {
        var sourceFolder = await CreateModifiableFolderWithItems(fileCount, folderCount);

        // If the item existed beforehand and we chose not to overwrite it, the "create" operation turns into an "open" operation.
        // Create an original item to check against created items, to determine if an overwrite happened.
        var originalFile = await sourceFolder.CreateFileAsync("name");

        // Create unique content, to check if overwrite happens
        var uniqueContent = await CopyRandomBytesToAsync(originalFile);

        var createdFile = await sourceFolder.CreateFileAsync(originalFile.Name, overwrite: true);
        using var createdFileStream = await createdFile.OpenStreamAsync();
        var createdFileBytes = await createdFileStream.ToBytesAsync();

        // Make sure the unique content doesn't exists.
        CollectionAssert.AreNotEqual(createdFileBytes, uniqueContent);
    }

    private async Task<byte[]> CopyRandomBytesToAsync(IFile file)
    {
        var randomBytes = new byte[1024];
        new Random().NextBytes(randomBytes);

        using var randomBytesStream = new MemoryStream(randomBytes);
        using var randomBytesTargetStream = await file.OpenStreamAsync(FileAccess.ReadWrite);
        await randomBytesStream.CopyToAsync(randomBytesTargetStream);
        return randomBytes;
    }

}