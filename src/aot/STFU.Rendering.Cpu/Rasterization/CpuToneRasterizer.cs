using STFU.NPR.Rendering;
using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.Abstractions.Surfaces;

namespace STFU.Rendering.Cpu.Rasterization;

public sealed class CpuToneRasterizer
{
    public void DrawToneSurface(
        PixelSurface target,
        NprToneSurface2D tone,
        float layerOpacity,
        NprFrameBudget budget)
    {
        if (tone.Width <= 0 || tone.Height <= 0 || tone.Rgba.Length < tone.Width * tone.Height * 4)
        {
            return;
        }

        var opacity = Math.Clamp(tone.Opacity * layerOpacity, 0f, 1f);
        if (opacity <= 0f)
        {
            return;
        }

        var workerCount = budget.ResolveWorkerCount();
        var parallel = budget.EnableTileParallelism && workerCount > 1;

        Action<int> drawRow = y =>
        {
            var sourceY = Math.Clamp((int)((long)y * tone.Height / target.Height), 0, tone.Height - 1);
            for (var x = 0; x < target.Width; x++)
            {
                var sourceX = Math.Clamp((int)((long)x * tone.Width / target.Width), 0, tone.Width - 1);
                var si = (sourceY * tone.Width + sourceX) * 4;

                var sourceAlpha = tone.Rgba[si + 3] / 255f * opacity;
                if (sourceAlpha <= 0f)
                {
                    continue;
                }

                var alpha = (byte)Math.Clamp((int)MathF.Round(sourceAlpha * 255f), 0, 255);
                var premulR = CpuPixelBlender.Premultiply(tone.Rgba[si], alpha);
                var premulG = CpuPixelBlender.Premultiply(tone.Rgba[si + 1], alpha);
                var premulB = CpuPixelBlender.Premultiply(tone.Rgba[si + 2], alpha);
                CpuPixelBlender.BlendSourceOverBgraPremultiplied(target, x, y, premulB, premulG, premulR, alpha);
            }
        };

        if (parallel)
        {
            Parallel.For(
                0,
                target.Height,
                new ParallelOptions { MaxDegreeOfParallelism = workerCount },
                drawRow);
        }
        else
        {
            for (var y = 0; y < target.Height; y++)
            {
                drawRow(y);
            }
        }
    }
}
