namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A value a component property takes either as a plain value or as a callback that is read all the time. Assign
/// either one, and the later assignment replaces the earlier. Reading the property gives its value now.
/// </summary>
/// <example>
/// <code>
/// button.Disabled = true;
/// button.Disabled = () => !_ready;
/// </code>
/// </example>
/// <typeparam name="T">The type of the value.</typeparam>
public readonly struct TimbnBinding<T>
{
    private readonly T? _value;
    private readonly Func<T>? _read;

    /// <summary>A plain value.</summary>
    /// <param name="value">The value.</param>
    public TimbnBinding(T value)
    {
        _value = value;
        _read = null;
    }

    /// <summary>A value that follows a callback.</summary>
    /// <param name="read">Returns the value now.</param>
    public TimbnBinding(Func<T> read)
    {
        _value = default;
        _read = read;
    }

    /// <summary>The value now.</summary>
    public T Current => _read != null ? _read() : _value!;

    /// <summary>A plain value as a binding.</summary>
    /// <param name="value">The value.</param>
    public static implicit operator TimbnBinding<T>(T value) => new(value);

    /// <summary>A callback as a binding.</summary>
    /// <param name="read">Returns the value now.</param>
    public static implicit operator TimbnBinding<T>(Func<T> read) => new(read);

    /// <summary>A binding as its value now.</summary>
    /// <param name="binding">The binding.</param>
    public static implicit operator T(TimbnBinding<T> binding) => binding.Current;
}
