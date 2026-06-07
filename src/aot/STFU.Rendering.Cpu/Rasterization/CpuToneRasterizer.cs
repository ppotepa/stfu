using STFU.Common.Math;
using STFU.NPR.Rendering;
using STFU.Parallelism;
using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.Abstractions.Surfaces;

namespace STFU.Rendering.Cpu.Rasterization;

public sealed class CpuToneRasterizer
{
    private readonly byte[] _alphaLut = new byte[256];
    private float _alphaLutOpacity = float.NaN;

    public void DrawToneSurface(
        PixelSurface target,
        NprToneSurface2D tone,
        float layerOpacity,
        NprFrameBudget budget,
        CpuRasterWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        if (tone.Width <= 0 || tone.Height <= 0 || tone.Rgba.Length < tone.Width * tone.Height * PixelMemoryMath.Bgra32BytesPerPixel)
        {
            return;
        }

        var opacity = ToneMath.EffectiveOpacity(tone.Opacity, layerOpacity);
        if (opacity <= 0f)
        {
            return;
        }

        var workerCount = budget.ResolveWorkerCount();
        var parallel = budget.EnableTileParallelism && workerCount > 1;
        var sameSize = tone.Width == target.Width && tone.Height == target.Height;
        var sourceXMap = sameSize ? null : GetSourceXMap(target.Width, tone.Width, workspace);
        var sourceYMap = sameSize ? null : GetSourceYMap(target.Height, tone.Height, workspace);
        var alphaLut = GetAlphaLut(opacity);

        if (parallel)
        {
            DeterministicParallel.ForRanges(
                0,
                target.Height,
                workerCount,
                cancellationToken,
                (start, end, _, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    if (sameSize)
                    {
                        DrawSameSizeRows(target, tone, alphaLut, start, end, token);
                    }
                    else
                    {
                        DrawMappedRows(target, tone, alphaLut, sourceXMap!, sourceYMap!, start, end, token);
                    }
                },
                minItemsPerRange: 16);
        }
        else if (sameSize)
        {
            DrawSameSizeRows(target, tone, alphaLut, 0, target.Height, cancellationToken);
        }
        else
        {
            DrawMappedRows(target, tone, alphaLut, sourceXMap!, sourceYMap!, 0, target.Height, cancellationToken);
        }
    }


    private static void DrawSameSizeRows(
        PixelSurface target,
        NprToneSurface2D tone,
        byte[] alphaLut,
        int startY,
        int endY,
        CancellationToken cancellationToken)
    {
        for (var y = startY; y < endY; y++)
        {
            if ((y & 0x3FF) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            var rowBase = PixelMemoryMath.Bgra32LinearIndex(0, y, tone.Width) * PixelMemoryMath.Bgra32BytesPerPixel;
            for (var x = 0; x < target.Width; x++)
            {
                BlendTonePixel(target, tone.Rgba, alphaLut, rowBase + x * PixelMemoryMath.Bgra32BytesPerPixel, x, y);
            }
        }
    }

    private static void DrawMappedRows(
        PixelSurface target,
        NprToneSurface2D tone,
        byte[] alphaLut,
        int[] sourceXMap,
        int[] sourceYMap,
        int startY,
        int endY,
        CancellationToken cancellationToken)
    {
        for (var y = startY; y < endY; y++)
        {
            if ((y & 0x3FF) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            var sourceY = sourceYMap[y];
            for (var x = 0; x < target.Width; x++)
            {
                var sourceX = sourceXMap[x];
                var sourceIndex = PixelMemoryMath.Bgra32LinearIndex(sourceX, sourceY, tone.Width) * PixelMemoryMath.Bgra32BytesPerPixel;
                BlendTonePixel(target, tone.Rgba, alphaLut, sourceIndex, x, y);
            }
        }
    }

    private static void BlendTonePixel(
        PixelSurface target,
        byte[] rgba,
        byte[] alphaLut,
        int sourceIndex,
        int x,
        int y)
    {
        var alpha = alphaLut[rgba[sourceIndex + 3]];
        if (alpha == 0)
        {
            return;
        }

        var premulR = ColorBlendMath.Premultiply(rgba[sourceIndex], alpha);
        var premulG = ColorBlendMath.Premultiply(rgba[sourceIndex + 1], alpha);
        var premulB = ColorBlendMath.Premultiply(rgba[sourceIndex + 2], alpha);
        CpuPixelBlender.BlendSourceOverBgraPremultiplied(target, x, y, premulB, premulG, premulR, alpha);
    }

    private static int[] GetSourceXMap(int targetWidth, int sourceWidth, CpuRasterWorkspace workspace)
    {
        return workspace.GetToneSourceXMap(targetWidth, sourceWidth);
    }

    private static int[] GetSourceYMap(int targetHeight, int sourceHeight, CpuRasterWorkspace workspace)
    {
        return workspace.GetToneSourceYMap(targetHeight, sourceHeight);
    }

    private byte[] GetAlphaLut(float opacity)
    {
        if (opacity.Equals(_alphaLutOpacity))
        {
            return _alphaLut;
        }

        for (var i = 0; i < _alphaLut.Length; i++)
        {
            _alphaLut[i] = ToneMath.ScaleAlpha((byte)i, opacity);
        }

        _alphaLutOpacity = opacity;
        return _alphaLut;
    }
}
