using STFU.Common.Primitives;

namespace STFU.NPR.Analysis;

public sealed class FrameProjectionCache
{
    private const int MaxEntries = 64;

    private readonly object _gate = new();
    private readonly Dictionary<FrameProjectionCacheKey, CacheEntry> _entries = new();
    private long _clock;

    public FrameProjectionCacheStats Stats { get; } = new();

    public bool TryGet(FrameProjectionCacheKey key, out ProjectedMeshFrame frame)
    {
        lock (_gate)
        {
            if (_entries.TryGetValue(key, out var entry))
            {
                entry.LastUsed = ++_clock;
                frame = entry.Frame;
                Stats.Hit();
                return true;
            }

            frame = null!;
            Stats.Miss();
            return false;
        }
    }

    public void Store(FrameProjectionCacheKey key, ProjectedMeshFrame frame)
    {
        lock (_gate)
        {
            if (_entries.Count >= MaxEntries && !_entries.ContainsKey(key))
            {
                var oldest = _entries
                    .OrderBy(pair => pair.Value.LastUsed)
                    .First()
                    .Key;

                _entries.Remove(oldest);
                Stats.Evict();
            }

            _entries[key] = new CacheEntry(frame, ++_clock);
            Stats.Entries = _entries.Count;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _entries.Clear();
            _clock = 0;
            Stats.Clear();
        }
    }

    public void RemoveMesh(MeshHandle mesh)
    {
        lock (_gate)
        {
            foreach (var key in _entries.Keys.Where(key => key.Mesh == mesh).ToArray())
            {
                _entries.Remove(key);
            }

            Stats.Entries = _entries.Count;
        }
    }

    private sealed class CacheEntry
    {
        public CacheEntry(ProjectedMeshFrame frame, long lastUsed)
        {
            Frame = frame;
            LastUsed = lastUsed;
        }

        public ProjectedMeshFrame Frame { get; }
        public long LastUsed { get; set; }
    }
}
