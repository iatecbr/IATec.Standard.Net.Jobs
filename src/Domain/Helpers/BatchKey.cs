namespace Domain.Helpers;

/// <summary>
///     Typed wrapper for batch progress tracking keys.
///     Prevents raw string usage and provides a consistent format.
/// </summary>
public readonly record struct BatchKey
{
    private BatchKey(string value)
    {
        Value = value;
    }

    public string Value { get; }

    /// <summary>
    ///     Creates a BatchKey from a Hangfire job ID.
    ///     Adds the "batch:progress:" prefix automatically.
    /// </summary>
    public static BatchKey FromJobId(string jobId)
    {
        return new BatchKey($"batch:progress:{jobId}");
    }

    /// <summary>
    ///     Creates a BatchKey from an already-formatted raw value (e.g., from storage).
    ///     Does NOT add any prefix — the value is used as-is.
    /// </summary>
    public static BatchKey FromRawValue(string rawValue)
    {
        return new BatchKey(rawValue);
    }

    /// <summary>
    ///     Creates a BatchKey with a new unique identifier.
    /// </summary>
    public static BatchKey CreateNew()
    {
        return new BatchKey($"batch:progress:{Guid.NewGuid()}");
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(BatchKey key)
    {
        return key.Value;
    }
}