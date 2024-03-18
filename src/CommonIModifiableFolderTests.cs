using Microsoft.VisualStudio.TestTools.UnitTesting;
using OwlCore.Extensions;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Diagnostics;

namespace OwlCore.Storage.CommonTests;

public abstract class CommonIModifiableFolderTests : CommonIFolderTests
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
        // Create a folder with only 2 items
        var folder = await CreateModifiableFolderWithItems(1, 1);
        var firstItem = await folder.GetItemsAsync().FirstAsync();

        // Delete the first item
        await folder.DeleteAsync(firstItem);
        
        // Retrieve the first item again
        var newFirstItem = await folder.GetItemsAsync().FirstOrDefaultAsync(x => x.Id == firstItem.Id);

        // Make sure the remaining item in the folder is not the first
        Assert.IsNull(newFirstItem, $"Created and deleted item with id '{firstItem.Id}' but the item is still present in the folder. Check the {folder.GetType()} implementation of {nameof(IModifiableFolder)}.{nameof(ICreateCopyOf.DeleteAsync)}.");
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
        using var copyStream = await copy.OpenReadAsync();
        var copiedBytes = await copyStream.ToBytesAsync();
        CollectionAssert.AreEqual(randomBytesAddedToOriginalFile, copiedBytes);

        // If the file already exists, and we chose not to overwrite it, a "FileAlreadyExistsException" should throw.
        await Assert.ThrowsExceptionAsync<FileAlreadyExistsException>(async () => await destinationFolder.CreateCopyOfAsync(copy, overwrite: false), $"If an item of the same name already exists in the destination folder, {nameof(FileAlreadyExistsException)} should be thrown. Check the {sourceFolder.GetType()} implementation of {nameof(ICreateCopyOf)}.{nameof(ICreateCopyOf.CreateCopyOfAsync)}.");
    }

    [TestMethod]
    public async Task MoveFromAsyncTest()
    {
        var sourceFolder = await CreateModifiableFolderWithItems(1, 0);
        var destinationFolder = await CreateModifiableFolderWithItems(1, 0);

        // Copy random bytes to original file
        var originalFile = await sourceFolder.GetFilesAsync().FirstAsync();
        var randomBytesAddedToOriginalFile = await CopyRandomBytesToAsync(originalFile);

        // Create copy
        var copy = await destinationFolder.MoveFromAsync(originalFile, sourceFolder, overwrite: true);

        // Read copy
        using var copyStream = await copy.OpenReadAsync();
        var copiedBytes = await copyStream.ToBytesAsync();
        CollectionAssert.AreEqual(randomBytesAddedToOriginalFile, copiedBytes);

        // If the file already exists, and we chose not to overwrite it, a "FileAlreadyExistsException" should throw.
        await Assert.ThrowsExceptionAsync<FileAlreadyExistsException>(async () => await destinationFolder.MoveFromAsync(copy, sourceFolder, overwrite: false), $"If an item of the same name already exists in the destination folder, {nameof(FileAlreadyExistsException)} should be thrown. Check the {sourceFolder.GetType()} implementation of {nameof(IMoveFrom)}.{nameof(IMoveFrom.MoveFromAsync)}.");
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

        Assert.AreEqual(folderCount + 1, await sourceFolder.GetFoldersAsync().CountAsync(), $"The created folder was not found in the folder it was created in. Check the {sourceFolder.GetType()} implementation of {nameof(IModifiableFolder)}.{nameof(IModifiableFolder.CreateFolderAsync)}.");
    }

    [DataRow(0, 0)]
    [DataRow(1, 0)]
    [DataRow(0, 1)]
    [DataRow(1, 1)]
    [TestMethod]
    public async Task CreateNewFolderAsyncTest_FolderWithItems_NameExists_NoOverwrite(int fileCount, int folderCount)
    {
        var sourceFolder = await CreateModifiableFolderWithItems(fileCount, folderCount);

        // If the folder existed beforehand and we chose not to overwrite it, the "create" operation turns into an "open" operation.
        // Create an original item to check against created items, to determine if an overwrite happened.
        var childFolder = (IModifiableFolder)await sourceFolder.CreateFolderAsync("name");

        // Create unique content inside the folder we're overwriting, to check if overwritten folder contents are empty.
        var uniqueContent = await childFolder.CreateFolderAsync($"{Guid.NewGuid()}");

        // Recreate childFolder, without overwriting it.
        var createdOrOpenedFolder = await sourceFolder.CreateFolderAsync(childFolder.Name, overwrite: false);
        var folderContents = await createdOrOpenedFolder.GetFoldersAsync().ToListAsync();

        // The folder should still contain the unique content.
        Assert.AreEqual(1, folderContents.Count, $"If {nameof(IModifiableFolder)}.{nameof(IModifiableFolder.CreateFolderAsync)} is called but the name is already in use, then when overwrite is true the created folder should be empty.");
        
        // Make sure the unique content is still in the folder.
        Assert.IsTrue(folderContents.Any(x => x.Id == uniqueContent.Id), $"If {nameof(IModifiableFolder)}.{nameof(IModifiableFolder.CreateFolderAsync)} is called but the name is already in use, then when overwrite is true the created folder should contain no previous content.");
    }

    [DataRow(0, 0)]
    [DataRow(1, 0)]
    [DataRow(0, 1)]
    [DataRow(1, 1)]
    [TestMethod]
    public async Task CreateNewFolderAsyncTest_FolderWithItems_NameExists_Overwrite(int fileCount, int folderCount)
    {
        var sourceFolder = await CreateModifiableFolderWithItems(fileCount, folderCount);

        // If the item existed beforehand and we chose not to overwrite it, the "create" operation turns into an "open" operation.
        // Create an original item to check against created items, to determine if an overwrite happened.
        var childFolder = await sourceFolder.CreateFolderAsync("name");
        
        // Create unique content that should remain unaffected.
        var uniqueContent = await ((IModifiableFolder)childFolder).CreateFolderAsync($"{Guid.NewGuid()}");
        
        // Recreate childFolder, overwriting it.
        var createdOrOpenedFolder = await sourceFolder.CreateFolderAsync(childFolder.Name, overwrite: true);
        var folderContents = await createdOrOpenedFolder.GetFoldersAsync().ToListAsync();

        // The created folder should be empty, no previous content.
        Assert.AreEqual(0, folderContents.Count, $"If {nameof(IModifiableFolder)}.{nameof(IModifiableFolder.CreateFolderAsync)} is called but the name is already in use, then when overwrite is true the created folder should be empty.");

        // Make sure the unique content no longer exists in the folder.
        Assert.IsTrue(folderContents.All(x => x.Name != uniqueContent.Name), $"If {nameof(IModifiableFolder)}.{nameof(IModifiableFolder.CreateFolderAsync)} is called but the name is already in use, then when overwrite is true the created folder should contain no previous content.");
    }

    [DataRow(0, 0)]
    [DataRow(1, 0)]
    [DataRow(0, 1)]
    [DataRow(1, 1)]
    [TestMethod]
    public async Task CreateNewFileAsyncTest_FolderWithItems_NameNotExists(int fileCount, int folderCount)
    {
        var sourceFolder = await CreateModifiableFolderWithItems(fileCount, folderCount);
        await sourceFolder.CreateFileAsync("name");

        Assert.AreEqual(fileCount + 1, await sourceFolder.GetFilesAsync().CountAsync(), $"The created file was not found in the folder it was created in. Check the {sourceFolder.GetType()} implementation of {nameof(IModifiableFolder)}.{nameof(IModifiableFolder.CreateFileAsync)}.");
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
        using var createdFileStream = await createdFile.OpenReadAsync();
        var createdFileBytes = await createdFileStream.ToBytesAsync();

        // Make sure the unique content still exists.
        CollectionAssert.AreEqual(createdFileBytes, uniqueContent, $"If {nameof(IModifiableFolder)}.{nameof(IModifiableFolder.CreateFileAsync)} is called but the name is already in use, then when overwrite is false the copy operation should turn into an open operation.");
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
        using var createdFileStream = await createdFile.OpenReadAsync();
        var createdFileBytes = await createdFileStream.ToBytesAsync();

        // Make sure the unique content doesn't exists.
        CollectionAssert.AreNotEqual(createdFileBytes, uniqueContent, $"If {nameof(IModifiableFolder)}.{nameof(IModifiableFolder.CreateFileAsync)} is called but the name is already in use, then when overwrite is true the file content should be empty.");
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