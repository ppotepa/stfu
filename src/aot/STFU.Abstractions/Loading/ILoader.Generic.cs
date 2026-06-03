namespace STFU.Abstractions.Loading;

public interface ILoader<TSource, TOutput> : ILoader
{
    LoadResult<TOutput> Load(TSource source, LoadContext context);
}
