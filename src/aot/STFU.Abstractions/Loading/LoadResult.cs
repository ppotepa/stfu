namespace STFU.Abstractions.Loading;

public readonly record struct LoadResult<T>(
    bool Success,
    T? Value,
    string? Error)
{
    public static LoadResult<T> Ok(T value)
    {
        return new LoadResult<T>(true, value, null);
    }

    public static LoadResult<T> Fail(string error)
    {
        return new LoadResult<T>(false, default, error);
    }

    public T GetValueOrThrow()
    {
        if (Success && Value is not null)
        {
            return Value;
        }

        throw new InvalidOperationException(Error ?? "Load operation failed.");
    }
}
