namespace STFU.NPR.Graph;

public sealed class DefaultFaceIdVisibilityBuffer
{
    public DefaultFaceIdVisibilityBuffer(int width, int height, int faceCount)
    {
        Width = Math.Max(8, width);
        Height = Math.Max(8, height);
        var pixelCount = Width * Height;

        Depth = new float[pixelCount];
        FaceId = new int[pixelCount];
        FaceVisible = new bool[Math.Max(0, faceCount)];

        Clear();
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
        Array.Fill(FaceVisible, false);
    }

    public int ToBufferX(float screenX, int viewportWidth)
    {
        return Math.Clamp(
            (int)MathF.Floor((screenX / Math.Max(1, viewportWidth)) * Width),
            0,
            Width - 1);
    }

    public int ToBufferY(float screenY, int viewportHeight)
    {
        return Math.Clamp(
            (int)MathF.Floor((screenY / Math.Max(1, viewportHeight)) * Height),
            0,
            Height - 1);
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
                if (faceId == firstAllowedFace || faceId == secondAllowedFace)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void MarkVisibleFaces()
    {
        Array.Fill(FaceVisible, false);

        for (var i = 0; i < FaceId.Length; i++)
        {
            var face = FaceId[i];
            if ((uint)face < (uint)FaceVisible.Length)
            {
                FaceVisible[face] = true;
            }
        }
    }

    public static float EdgeFunction(float ax, float ay, float bx, float by, float px, float py)
    {
        return (px - ax) * (by - ay) - (py - ay) * (bx - ax);
    }
}
