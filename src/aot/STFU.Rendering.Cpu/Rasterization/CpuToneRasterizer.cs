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

        Action<int> drawRow = y =>
        {
            var sourceY = sameSize ? y : sourceYMap![y];
            for (var x = 0; x < target.Width; x++)
            {
                var sourceX = sameSize ? x : sourceXMap![x];
                var si = PixelMemoryMath.Bgra32LinearIndex(sourceX, sourceY, tone.Width) * PixelMemoryMath.Bgra32BytesPerPixel;

                var alpha = alphaLut[tone.Rgba[si + 3]];
                if (alpha == 0)
                {
                    continue;
                }

                var premulR = ColorBlendMath.Premultiply(tone.Rgba[si], alpha);
                var premulG = ColorBlendMath.Premultiply(tone.Rgba[si + 1], alpha);
                var premulB = ColorBlendMath.Premultiply(tone.Rgba[si + 2], alpha);
                CpuPixelBlender.BlendSourceOverBgraPremultiplied(target, x, y, premulB, premulG, premulR, alpha);
            }
        };

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
                    for (var y = start; y < end; y++)
                    {
                        if ((y & 0x3FF) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        drawRow(y);
                    }
                },
                minItemsPerRange: 16);
        }
        else
        {
            for (var y = 0; y < target.Height; y++)
            {
                drawRow(y);
            }
        }
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
