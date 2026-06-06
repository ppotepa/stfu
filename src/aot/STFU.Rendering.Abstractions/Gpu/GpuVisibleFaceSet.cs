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
