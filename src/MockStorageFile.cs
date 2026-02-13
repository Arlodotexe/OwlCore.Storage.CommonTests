using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.CommonTests;

/// <summary>
/// A mock file implementation for testing cross-implementation scenarios.
/// Supports configurable timestamp property support.
/// </summary>
public class MockStorageFile : IChildFile, ICreatedAt, ICreatedAtOffset, ILastModifiedAt, ILastModifiedAtOffset, ILastAccessedAt, ILastAccessedAtOffset
{
    private readonly MockStorageFolder _parent;
    private byte[] _content;

    public MockStorageFile(string name, MockStorageFolder parent, byte[]? content = null)
    {
        Name = name;
        _parent = parent;
        _content = content ?? Array.Empty<byte>();

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

    public string Id => $"{_parent.Id}/{Name}";
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

    public Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
    {
        LastAccessedAtValue = DateTimeOffset.UtcNow;

        if (accessMode == FileAccess.Read)
            return Task.FromResult<Stream>(new MemoryStream(_content, writable: false));

        // For write access, return a stream that captures writes
        var stream = new MockWriteStream(_content, bytes =>
        {
            _content = bytes;
            LastModifiedAtValue = DateTimeOffset.UtcNow;
        });

        return Task.FromResult<Stream>(stream);
    }

    internal void SetContent(byte[] content)
    {
        _content = content;
    }

    private class MockWriteStream : MemoryStream
    {
        private readonly Action<byte[]> _onDispose;

        public MockWriteStream(byte[] initial, Action<byte[]> onDispose) : base()
        {
            Write(initial, 0, initial.Length);
            Position = 0;
            _onDispose = onDispose;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _onDispose(ToArray());
            base.Dispose(disposing);
        }
    }
}

/// <summary>
/// A mock file that has NO timestamp property support.
/// Used for testing cross-implementation scenarios with partial property support.
/// </summary>
public class MockStorageFileNoTimestamps : IChildFile
{
    private readonly IFolder? _parent;
    private byte[] _content;

    public MockStorageFileNoTimestamps(string name, IFolder? parent, byte[]? content = null)
    {
        Name = name;
        _parent = parent;
        _content = content ?? Array.Empty<byte>();
    }

    public string Id => _parent != null ? $"{_parent.Id}/{Name}" : $"mock-no-ts://{Name}";
    public string Name { get; }

    public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_parent);

    public Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
    {
        if (accessMode == FileAccess.Read)
            return Task.FromResult<Stream>(new MemoryStream(_content, writable: false));

        var stream = new MemoryStream();
        stream.Write(_content, 0, _content.Length);
        stream.Position = 0;
        return Task.FromResult<Stream>(stream);
    }

    internal void SetContent(byte[] content)
    {
        _content = content;
    }
}
