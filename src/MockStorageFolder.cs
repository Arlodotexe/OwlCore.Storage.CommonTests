using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.CommonTests;

/// <summary>
/// A mock folder implementation for testing cross-implementation scenarios.
/// Does NOT implement ICreateCopyOf or IMoveFrom, forcing fallback paths.
/// </summary>
public class MockStorageFolder : IModifiableFolder, IChildFolder, ICreatedAt, ICreatedAtOffset, ILastModifiedAt, ILastModifiedAtOffset, ILastAccessedAt, ILastAccessedAtOffset
{
    private readonly MockStorageFolder? _parent;
    private readonly List<IStorableChild> _children = new();
    private readonly bool _createFilesWithTimestamps;

    public MockStorageFolder(string name, MockStorageFolder? parent = null, bool createFilesWithTimestamps = true)
    {
        Name = name;
        _parent = parent;
        _createFilesWithTimestamps = createFilesWithTimestamps;

        var now = DateTimeOffset.UtcNow;
        CreatedAtValue = now;
        LastModifiedAtValue = now;
        LastAccessedAtValue = now;

        CreatedAt = new MockCreatedAtProperty(
            $"{Id}/{nameof(ICreatedAt.CreatedAt)}",
            () => CreatedAtValue?.LocalDateTime,
            v => CreatedAtValue = v.HasValue ? new DateTimeOffset(v.Value) : null);

        CreatedAtOffset = new MockCreatedAtOffsetProperty(
            $"{Id}/{nameof(ICreatedAtOffset.CreatedAtOffset)}",
            () => CreatedAtValue,
            v => CreatedAtValue = v);

        LastModifiedAt = new MockLastModifiedAtProperty(
            $"{Id}/{nameof(ILastModifiedAt.LastModifiedAt)}",
            () => LastModifiedAtValue?.LocalDateTime,
            v => LastModifiedAtValue = v.HasValue ? new DateTimeOffset(v.Value) : null);

        LastModifiedAtOffset = new MockLastModifiedAtOffsetProperty(
            $"{Id}/{nameof(ILastModifiedAtOffset.LastModifiedAtOffset)}",
            () => LastModifiedAtValue,
            v => LastModifiedAtValue = v);

        LastAccessedAt = new MockLastAccessedAtProperty(
            $"{Id}/{nameof(ILastAccessedAt.LastAccessedAt)}",
            () => LastAccessedAtValue?.LocalDateTime,
            v => LastAccessedAtValue = v.HasValue ? new DateTimeOffset(v.Value) : null);

        LastAccessedAtOffset = new MockLastAccessedAtOffsetProperty(
            $"{Id}/{nameof(ILastAccessedAtOffset.LastAccessedAtOffset)}",
            () => LastAccessedAtValue,
            v => LastAccessedAtValue = v);
    }

    public string Id => _parent == null ? $"mock://{Name}" : $"{_parent.Id}/{Name}";
    public string Name { get; }

    public DateTimeOffset? CreatedAtValue { get; set; }
    public DateTimeOffset? LastModifiedAtValue { get; set; }
    public DateTimeOffset? LastAccessedAtValue { get; set; }

    public ICreatedAtProperty CreatedAt { get; }
    public ICreatedAtOffsetProperty CreatedAtOffset { get; }
    public ILastModifiedAtProperty LastModifiedAt { get; }
    public ILastModifiedAtOffsetProperty LastModifiedAtOffset { get; }
    public ILastAccessedAtProperty LastAccessedAt { get; }
    public ILastAccessedAtOffsetProperty LastAccessedAtOffset { get; }

    public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IFolder?>(_parent);

    public async IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LastAccessedAtValue = DateTimeOffset.UtcNow;

        foreach (var child in _children)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (type == StorableType.All)
                yield return child;
            else if (type == StorableType.File && child is IFile)
                yield return child;
            else if (type == StorableType.Folder && child is IFolder)
                yield return child;
        }
    }

    public Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Mock does not support folder watchers");

    public Task<IChildFile> CreateFileAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var existing = _children.OfType<IChildFile>().FirstOrDefault(f => f.Name == name);
        
        if (existing != null)
        {
            if (!overwrite)
                throw new FileAlreadyExistsException(name);
            _children.Remove((IStorableChild)existing);
        }

        IChildFile newFile = _createFilesWithTimestamps 
            ? new MockStorageFile(name, this)
            : new MockStorageFileNoTimestamps(name, this);
        
        _children.Add((IStorableChild)newFile);
        LastModifiedAtValue = DateTimeOffset.UtcNow;

        return Task.FromResult(newFile);
    }

    public Task<IChildFolder> CreateFolderAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var existing = _children.OfType<IChildFolder>().FirstOrDefault(f => f.Name == name);
        
        if (existing != null)
        {
            if (!overwrite)
                throw new InvalidOperationException($"Folder already exists: {name}");
            _children.Remove((IStorableChild)existing);
        }

        var newFolder = new MockStorageFolder(name, this, _createFilesWithTimestamps);
        _children.Add(newFolder);
        LastModifiedAtValue = DateTimeOffset.UtcNow;

        return Task.FromResult<IChildFolder>(newFolder);
    }

    public Task DeleteAsync(IStorableChild item, CancellationToken cancellationToken = default)
    {
        var existing = _children.FirstOrDefault(c => c.Id == item.Id);
        if (existing == null)
            throw new FileNotFoundException($"Item not found: {item.Name}");

        _children.Remove(existing);
        LastModifiedAtValue = DateTimeOffset.UtcNow;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds an existing file to this folder (for test setup).
    /// </summary>
    public void AddChild(IStorableChild child)
    {
        _children.Add(child);
    }

    /// <summary>
    /// Creates a pre-populated mock folder with items for testing.
    /// </summary>
    public static MockStorageFolder CreateWithItems(string name, int fileCount, int folderCount, bool filesWithTimestamps = true)
    {
        var folder = new MockStorageFolder(name, null, filesWithTimestamps);

        for (int i = 0; i < fileCount; i++)
        {
            IStorableChild file = filesWithTimestamps
                ? new MockStorageFile($"file_{i}.txt", folder, global::System.Text.Encoding.UTF8.GetBytes($"content {i}"))
                : new MockStorageFileNoTimestamps($"file_{i}.txt", folder, global::System.Text.Encoding.UTF8.GetBytes($"content {i}"));
            folder.AddChild(file);
        }

        for (int i = 0; i < folderCount; i++)
        {
            var subfolder = new MockStorageFolder($"folder_{i}", folder, filesWithTimestamps);
            folder.AddChild(subfolder);
        }

        return folder;
    }
}

/// <summary>
/// A mock folder that creates files WITHOUT timestamp support.
/// Used for testing cross-implementation scenarios with partial property support.
/// </summary>
public class MockStorageFolderNoTimestamps : IModifiableFolder, IChildFolder
{
    private readonly MockStorageFolderNoTimestamps? _parent;
    private readonly List<IStorableChild> _children = new();

    public MockStorageFolderNoTimestamps(string name, MockStorageFolderNoTimestamps? parent = null)
    {
        Name = name;
        _parent = parent;
    }

    public string Id => _parent == null ? $"mock-no-ts://{Name}" : $"{_parent.Id}/{Name}";
    public string Name { get; }

    public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IFolder?>(_parent);

    public async IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var child in _children)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (type == StorableType.All)
                yield return child;
            else if (type == StorableType.File && child is IFile)
                yield return child;
            else if (type == StorableType.Folder && child is IFolder)
                yield return child;
        }
    }

    public Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Mock does not support folder watchers");

    public Task<IChildFile> CreateFileAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var existing = _children.OfType<IChildFile>().FirstOrDefault(f => f.Name == name);
        
        if (existing != null)
        {
            if (!overwrite)
                throw new FileAlreadyExistsException(name);
            _children.Remove((IStorableChild)existing);
        }

        var newFile = new MockStorageFileNoTimestamps(name, this);
        _children.Add((IStorableChild)newFile);

        return Task.FromResult<IChildFile>(newFile);
    }

    public Task<IChildFolder> CreateFolderAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var existing = _children.OfType<IChildFolder>().FirstOrDefault(f => f.Name == name);
        
        if (existing != null)
        {
            if (!overwrite)
                throw new InvalidOperationException($"Folder already exists: {name}");
            _children.Remove((IStorableChild)existing);
        }

        var newFolder = new MockStorageFolderNoTimestamps(name, this);
        _children.Add(newFolder);

        return Task.FromResult<IChildFolder>(newFolder);
    }

    public Task DeleteAsync(IStorableChild item, CancellationToken cancellationToken = default)
    {
        var existing = _children.FirstOrDefault(c => c.Id == item.Id);
        if (existing == null)
            throw new FileNotFoundException($"Item not found: {item.Name}");

        _children.Remove(existing);
        return Task.CompletedTask;
    }

    public void AddChild(IStorableChild child)
    {
        _children.Add(child);
    }

    public static MockStorageFolderNoTimestamps CreateWithItems(string name, int fileCount, int folderCount)
    {
        var folder = new MockStorageFolderNoTimestamps(name);

        for (int i = 0; i < fileCount; i++)
        {
            var file = new MockStorageFileNoTimestamps($"file_{i}.txt", folder);
            folder.AddChild((IStorableChild)file);
        }

        for (int i = 0; i < folderCount; i++)
        {
            var subfolder = new MockStorageFolderNoTimestamps($"folder_{i}", folder);
            folder.AddChild(subfolder);
        }

        return folder;
    }
}
