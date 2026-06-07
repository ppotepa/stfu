namespace STFU.Rendering.Abstractions.Gpu;

public sealed class GpuVisibleFaceSet
{
    public GpuVisibleFaceSet(int faceCount, byte[] bits)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(faceCount);
        FaceCount = faceCount;
        Bits = bits ?? throw new ArgumentNullException(nameof(bits));
    }

    public int FaceCount { get; }

    public byte[] Bits { get; }

    public int CountVisible()
    {
        var count = 0;
        for (var byteIndex = 0; byteIndex < Bits.Length; byteIndex++)
        {
            var value = Bits[byteIndex];
            while (value != 0)
            {
                count += value & 1;
                value >>= 1;
            }
        }

        return Math.Min(count, FaceCount);
    }

    public bool IsVisible(int faceIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(faceIndex);
        if (faceIndex >= FaceCount)
        {
            throw new ArgumentOutOfRangeException(nameof(faceIndex));
        }

        var byteIndex = faceIndex >> 3;
        var bit = faceIndex & 7;
        return byteIndex < Bits.Length && (Bits[byteIndex] & (1 << bit)) != 0;
    }
}
