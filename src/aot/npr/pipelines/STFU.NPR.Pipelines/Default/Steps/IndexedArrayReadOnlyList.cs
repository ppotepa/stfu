namespace STFU.NPR.Pipeline.Default.Steps;

internal sealed class IndexedArrayReadOnlyList<T>(T[] items, int[] indices, int count) : IReadOnlyList<T>
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

internal sealed class ArraySliceReadOnlyList<T>(T[] items, int count) : IReadOnlyList<T>
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
