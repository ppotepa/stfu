using System.Numerics;
using STFU.Common.Math;
using STFU.Engine.Entities;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveFrameHasher
{
    private const ulong Offset = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public static ulong Empty => Offset;

    public static ulong Mix(ulong hash, bool value)
    {
        return Mix(hash, value ? 1UL : 0UL);
    }

    public static ulong Mix(ulong hash, int value)
    {
        return Mix(hash, unchecked((uint)value));
    }

    public static ulong Mix(ulong hash, long value)
    {
        return Mix(hash, unchecked((ulong)value));
    }

    public static ulong Mix(ulong hash, float value)
    {
        return Mix(hash, unchecked((uint)BitConverter.SingleToInt32Bits(value)));
    }

    public static ulong Mix(ulong hash, Vector3 value)
    {
        hash = Mix(hash, value.X);
        hash = Mix(hash, value.Y);
        hash = Mix(hash, value.Z);
        return hash;
    }

    public static ulong Mix(ulong hash, Transform3D value)
    {
        hash = Mix(hash, value.Position);
        hash = Mix(hash, value.Rotation);
        hash = Mix(hash, value.Scale);
        return hash;
    }

    public static ulong Mix(ulong hash, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return Mix(hash, 0);
        }

        foreach (var ch in value)
        {
            hash = Mix(hash, ch);
        }

        return hash;
    }

    public static ulong MixEntity(ulong hash, Entity entity)
    {
        hash = Mix(hash, entity.Id.Value);
        hash = Mix(hash, entity.Mesh.Value);
        hash = Mix(hash, entity.Name);
        hash = Mix(hash, entity.Transform);
        return hash;
    }

    public static ulong Mix(ulong hash, ulong value)
    {
        unchecked
        {
            hash ^= value;
            hash *= Prime;
            return hash;
        }
    }
}
