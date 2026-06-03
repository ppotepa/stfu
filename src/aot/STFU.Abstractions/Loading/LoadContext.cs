namespace STFU.Abstractions.Loading;

public sealed class LoadContext
{
    public static LoadContext Default { get; } = new();

    private readonly Dictionary<string, object> _items = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, object> Items => _items;

    public LoadContext Set<TValue>(string key, TValue value)
        where TValue : notnull
    {
        _items[key] = value;
        return this;
    }

    public bool TryGet<TValue>(string key, out TValue value)
        where TValue : notnull
    {
        if (_items.TryGetValue(key, out var raw) && raw is TValue typed)
        {
            value = typed;
            return true;
        }

        value = default!;
        return false;
    }
}
