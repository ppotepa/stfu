namespace STFU.Common.Collections;

/// <summary>
/// Exposes selected items from an array through an index array without allocating or copying the selected values.
/// </summary>
public sealed class IndexedArrayReadOnlyList<T>(T[] items, int[] indices, int count) : IReadOnlyList<T>
{
    public int Count => count;

    public T this[int index] => items[indices[index]];

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < count; i++)
        {
            yield return items[indices[i]];
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

/// <summary>
/// Exposes the first <paramref name="count" /> items of an array as a read-only list without allocating or copying.
/// </summary>
public sealed class ArraySliceReadOnlyList<T>(T[] items, int count) : IReadOnlyList<T>
{
    public int Count => count;

    public T this[int index] => items[index];

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < count; i++)
        {
            yield return items[i];
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
