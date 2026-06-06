namespace STFU.Common.Math;

public static class HashMath
{
    public const ulong FnvOffset64 = 14695981039346656037UL;

    public const ulong FnvPrime64 = 1099511628211UL;

    public static ulong Fnv1A(ulong current, int value)
    {
        unchecked
        {
            return (current ^ (uint)value) * FnvPrime64;
        }
    }

    public static ulong Fnv1A(ulong current, byte value)
    {
        unchecked
        {
            return (current ^ value) * FnvPrime64;
        }
    }

    public static ulong Fnv1A(ReadOnlySpan<byte> bytes)
    {
        var hash = FnvOffset64;
        for (var i = 0; i < bytes.Length; i++)
        {
            hash = Fnv1A(hash, bytes[i]);
        }

        return hash;
    }

    public static ulong Fnv1A(ulong current, uint value)
    {
        current = Fnv1A(current, (byte)value);
        current = Fnv1A(current, (byte)(value >> 8));
        current = Fnv1A(current, (byte)(value >> 16));
        return Fnv1A(current, (byte)(value >> 24));
    }

    public static ulong Fnv1A(ulong current, long value)
    {
        return Fnv1A(current, (ulong)value);
    }

    public static ulong Fnv1A(ulong current, ulong value)
    {
        current = Fnv1A(current, (byte)value);
        current = Fnv1A(current, (byte)(value >> 8));
        current = Fnv1A(current, (byte)(value >> 16));
        current = Fnv1A(current, (byte)(value >> 24));
        current = Fnv1A(current, (byte)(value >> 32));
        current = Fnv1A(current, (byte)(value >> 40));
        current = Fnv1A(current, (byte)(value >> 48));
        return Fnv1A(current, (byte)(value >> 56));
    }

    public static ulong Fnv1A(ulong current, float value)
    {
        return Fnv1A(current, BitConverter.SingleToUInt32Bits(value));
    }

    public static ulong Fnv1A(ulong current, double value)
    {
        return Fnv1A(current, (ulong)BitConverter.DoubleToInt64Bits(value));
    }

    public static ulong Fnv1A(ulong current, string? value)
    {
        if (value is null)
        {
            return Fnv1A(current, -1);
        }

        current = Fnv1A(current, value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            current = Fnv1A(current, (ushort)value[i]);
        }

        return current;
    }

    public static ulong Fnv1A(ulong current, ushort value)
    {
        current = Fnv1A(current, (byte)value);
        return Fnv1A(current, (byte)(value >> 8));
    }

    public static int Mix(int a, int b)
    {
        unchecked
        {
            var value = (uint)a;
            value ^= (uint)b + 0x9E3779B9u + (value << 6) + (value >> 2);
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return (int)value;
        }
    }

    public static int StableTriple(int a, int b, int c)
    {
        unchecked
        {
            return a * 397 ^ (b * 17) ^ c;
        }
    }

    public static int StablePerTriangleEdge(int triangleStableId, int edgeIndex, int startVertexIndex, int endVertexIndex)
    {
        unchecked
        {
            return (triangleStableId * 397) ^ (edgeIndex * 131) ^ startVertexIndex ^ (endVertexIndex * 17);
        }
    }

    public static int StableUndirectedEdge(int triangleStableId, int startVertexIndex, int endVertexIndex)
    {
        unchecked
        {
            return (triangleStableId * 397) ^ (startVertexIndex * 17) ^ endVertexIndex;
        }
    }

    public static int StableSequence31(int a, int b, int c)
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + a;
            hash = hash * 31 + b;
            hash = hash * 31 + c;
            return hash;
        }
    }

    public static float Float01From24Bits(int seed)
    {
        return (Mix(seed, 0) & 0x00FFFFFF) / 16777215f;
    }

    public static float SignedFloat01(int seed, int channel)
    {
        return Float01From24Bits(Mix(seed, channel)) * 2f - 1f;
    }
}
