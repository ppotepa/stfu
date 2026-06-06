using STFU.Common.Math;

namespace STFU.NPR.Graph;

public sealed class DefaultFaceIdVisibilityBuffer
{
    public DefaultFaceIdVisibilityBuffer(int width, int height, int faceCount)
    {
        Width = RasterMath.AtLeastPixels(width, 8);
        Height = RasterMath.AtLeastPixels(height, 8);
        var pixelCount = Width * Height;

        Depth = new float[pixelCount];
        FaceId = new int[pixelCount];
        FaceVisible = new bool[NumericMath.AtLeast(faceCount, 0)];

        Clear();
        Array.Fill(FaceVisible, false);
    }

    public int Width { get; }

    public int Height { get; }

    public float[] Depth { get; }

    public int[] FaceId { get; }

    public bool[] FaceVisible { get; }

    public int FaceCapacity => FaceVisible.Length;

    public void Clear()
    {
        Array.Fill(Depth, float.PositiveInfinity);
        Array.Fill(FaceId, -1);
    }

    public int ToBufferX(float screenX, int viewportWidth)
    {
        return RasterMath.ToBufferCoordinate(screenX, viewportWidth, Width);
    }

    public int ToBufferY(float screenY, int viewportHeight)
    {
        return RasterMath.ToBufferCoordinate(screenY, viewportHeight, Height);
    }

    public bool SampleOwnedFaceAtScreen(
        float screenX,
        float screenY,
        int viewportWidth,
        int viewportHeight,
        IReadOnlySet<int> allowedFaces)
    {
        var cx = ToBufferX(screenX, viewportWidth);
        var cy = ToBufferY(screenY, viewportHeight);
        return SampleOwnedFaceAtBuffer(cx, cy, allowedFaces);
    }

    public bool SampleOwnedFaceAtBuffer(int cx, int cy, IReadOnlySet<int> allowedFaces)
    {
        if (allowedFaces.Count == 0)
        {
            return false;
        }

        for (var dy = -1; dy <= 1; dy++)
        {
            var yy = cy + dy;
            if ((uint)yy >= (uint)Height)
            {
                continue;
            }

            for (var dx = -1; dx <= 1; dx++)
            {
                var xx = cx + dx;
                if ((uint)xx >= (uint)Width)
                {
                    continue;
                }

                var faceId = FaceId[yy * Width + xx];
                if (allowedFaces.Contains(faceId))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool SampleOwnedFaceAtScreen(
        float screenX,
        float screenY,
        int viewportWidth,
        int viewportHeight,
        int firstAllowedFace,
        int secondAllowedFace)
    {
        var cx = ToBufferX(screenX, viewportWidth);
        var cy = ToBufferY(screenY, viewportHeight);
        return SampleOwnedFaceAtBuffer(cx, cy, firstAllowedFace, secondAllowedFace);
    }

    public bool SampleOwnedFaceAtBuffer(int cx, int cy, int firstAllowedFace, int secondAllowedFace)
    {

        for (var dy = -1; dy <= 1; dy++)
        {
            var yy = cy + dy;
            if ((uint)yy >= (uint)Height)
            {
                continue;
            }

            for (var dx = -1; dx <= 1; dx++)
            {
                var xx = cx + dx;
                if ((uint)xx >= (uint)Width)
                {
                    continue;
                }

                var faceId = FaceId[yy * Width + xx];
                if ((firstAllowedFace >= 0 && faceId == firstAllowedFace) ||
                    (secondAllowedFace >= 0 && faceId == secondAllowedFace))
                {
                    return true;
                }
            }
        }

        return false;
    }
    public void MarkVisibleFaces(bool clear = true)
    {
        if (clear)
        {
            Array.Fill(FaceVisible, false);
        }

        var faceId = FaceId;
        var faceVisible = FaceVisible;
        for (var i = 0; i < faceId.Length; i++)
        {
            var face = faceId[i];
            if ((uint)face < (uint)faceVisible.Length)
            {
                faceVisible[face] = true;
            }
        }
    }

    public static float EdgeFunction(float ax, float ay, float bx, float by, float px, float py)
    {
        return RasterMath.EdgeFunction(ax, ay, bx, by, px, py);
    }
}
