using System;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.CommonTests;

/// <summary>
/// A simple mock modifiable property for testing.
/// </summary>
public class MockModifiableProperty<T> : IModifiableStorageProperty<T>
{
    private readonly Func<T> _getter;
    private readonly Action<T> _setter;

    public MockModifiableProperty(string id, string name, Func<T> getter, Action<T> setter)
    {
        Id = id;
        Name = name;
        _getter = getter;
        _setter = setter;
    }

    public string Id { get; }
    public string Name { get; }

    public Task<T> GetValueAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_getter());

    public Task UpdateValueAsync(T value, CancellationToken cancellationToken = default)
    {
        _setter(value);
        return Task.CompletedTask;
    }

    public Task<IStoragePropertyWatcher<T>> GetWatcherAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Mock does not support watchers");
}

/// <summary>
/// Concrete mock property implementations for timestamp interfaces.
/// </summary>
public class MockCreatedAtProperty : MockModifiableProperty<DateTime?>, IModifiableCreatedAtProperty
{
    public MockCreatedAtProperty(string id, Func<DateTime?> getter, Action<DateTime?> setter) 
        : base(id, nameof(ICreatedAt.CreatedAt), getter, setter) { }
}

public class MockCreatedAtOffsetProperty : MockModifiableProperty<DateTimeOffset?>, IModifiableCreatedAtOffsetProperty
{
    public MockCreatedAtOffsetProperty(string id, Func<DateTimeOffset?> getter, Action<DateTimeOffset?> setter) 
        : base(id, nameof(ICreatedAtOffset.CreatedAtOffset), getter, setter) { }
}

public class MockLastModifiedAtProperty : MockModifiableProperty<DateTime?>, IModifiableLastModifiedAtProperty
{
    public MockLastModifiedAtProperty(string id, Func<DateTime?> getter, Action<DateTime?> setter) 
        : base(id, nameof(ILastModifiedAt.LastModifiedAt), getter, setter) { }
}

public class MockLastModifiedAtOffsetProperty : MockModifiableProperty<DateTimeOffset?>, IModifiableLastModifiedAtOffsetProperty
{
    public MockLastModifiedAtOffsetProperty(string id, Func<DateTimeOffset?> getter, Action<DateTimeOffset?> setter) 
        : base(id, nameof(ILastModifiedAtOffset.LastModifiedAtOffset), getter, setter) { }
}

public class MockLastAccessedAtProperty : MockModifiableProperty<DateTime?>, IModifiableLastAccessedAtProperty
{
    public MockLastAccessedAtProperty(string id, Func<DateTime?> getter, Action<DateTime?> setter) 
        : base(id, nameof(ILastAccessedAt.LastAccessedAt), getter, setter) { }
}

public class MockLastAccessedAtOffsetProperty : MockModifiableProperty<DateTimeOffset?>, IModifiableLastAccessedAtOffsetProperty
{
    public MockLastAccessedAtOffsetProperty(string id, Func<DateTimeOffset?> getter, Action<DateTimeOffset?> setter) 
        : base(id, nameof(ILastAccessedAtOffset.LastAccessedAtOffset), getter, setter) { }
}
