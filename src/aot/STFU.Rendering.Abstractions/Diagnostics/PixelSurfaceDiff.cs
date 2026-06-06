using STFU.Common.Math;
using STFU.Rendering.Abstractions.Surfaces;

namespace STFU.Rendering.Abstractions.Diagnostics;

public static class PixelSurfaceDiff
{
    public static PixelSurfaceDiffResult Compare(PixelSurface left, PixelSurface right, byte tolerance = 0)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.Width != right.Width ||
            left.Height != right.Height ||
            left.Stride != right.Stride ||
            left.Format != right.Format)
        {
            throw new InvalidOperationException("Pixel surfaces must have identical dimensions, stride, and format.");
        }

        var differentPixelCount = 0;
        var maxChannelDelta = 0;
        long sumAbsoluteDelta = 0;
        int? firstDifferentX = null;
        int? firstDifferentY = null;
        string? firstDifferentChannel = null;

        for (var y = 0; y < left.Height; y++)
        {
            var leftRow = y * left.Stride;
            var rightRow = y * right.Stride;
            for (var x = 0; x < left.Width; x++)
            {
                var offset = x * 4;
                var bDelta = MetricMath.AbsoluteDelta(left.Pixels[leftRow + offset], right.Pixels[rightRow + offset]);
                var gDelta = MetricMath.AbsoluteDelta(left.Pixels[leftRow + offset + 1], right.Pixels[rightRow + offset + 1]);
                var rDelta = MetricMath.AbsoluteDelta(left.Pixels[leftRow + offset + 2], right.Pixels[rightRow + offset + 2]);
                var aDelta = MetricMath.AbsoluteDelta(left.Pixels[leftRow + offset + 3], right.Pixels[rightRow + offset + 3]);

                var pixelDiffers = bDelta > tolerance || gDelta > tolerance || rDelta > tolerance || aDelta > tolerance;
                if (!pixelDiffers)
                {
                    continue;
                }

                differentPixelCount++;
                sumAbsoluteDelta += bDelta + gDelta + rDelta + aDelta;

                if (firstDifferentX is null)
                {
                    firstDifferentX = x;
                    firstDifferentY = y;
                    firstDifferentChannel = FirstChannelName(bDelta, gDelta, rDelta, aDelta, tolerance);
                }

                if (bDelta > maxChannelDelta)
                {
                    maxChannelDelta = bDelta;
                }

                if (gDelta > maxChannelDelta)
                {
                    maxChannelDelta = gDelta;
                }

                if (rDelta > maxChannelDelta)
                {
                    maxChannelDelta = rDelta;
                }

                if (aDelta > maxChannelDelta)
                {
                    maxChannelDelta = aDelta;
                }
            }
        }

        return new PixelSurfaceDiffResult(
            differentPixelCount,
            maxChannelDelta,
            sumAbsoluteDelta,
            firstDifferentX,
            firstDifferentY,
            firstDifferentChannel);
    }

    private static string? FirstChannelName(int bDelta, int gDelta, int rDelta, int aDelta, byte tolerance)
    {
        if (bDelta > tolerance)
        {
            return "B";
        }

        if (gDelta > tolerance)
        {
            return "G";
        }

        if (rDelta > tolerance)
        {
            return "R";
        }

        if (aDelta > tolerance)
        {
            return "A";
        }

        return null;
    }
}

public sealed record PixelSurfaceDiffResult(
    int DifferentPixelCount,
    int MaxChannelDelta,
    long SumAbsoluteDelta,
    int? FirstDifferentX,
    int? FirstDifferentY,
    string? FirstDifferentChannel);
