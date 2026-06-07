using STFU.Common.Math;

namespace STFU.UI;

internal enum ViewportSurfaceMode
{
    Bitmap,
    DirectCandidate,
    DirectActive,
    DirectSuppressed
}

internal sealed class ViewportSurfaceRouter
{
    private readonly Func<(int Width, int Height)> _fallbackSizeProvider;
    private Func<(int Width, int Height)>? _directSizeProvider;

    public ViewportSurfaceRouter(Func<(int Width, int Height)> fallbackSizeProvider)
    {
        _fallbackSizeProvider = fallbackSizeProvider;
    }

    public ViewportSurfaceMode Mode { get; private set; } = ViewportSurfaceMode.Bitmap;

    public bool DirectSurfaceActive =>
        Mode is ViewportSurfaceMode.DirectCandidate or ViewportSurfaceMode.DirectActive;

    public bool ShowDirectHost => Mode == ViewportSurfaceMode.DirectActive;

    public bool DrawBitmap => Mode != ViewportSurfaceMode.DirectActive;

    public void ApplyPlan(RendererRuntimePlan plan)
    {
        Mode = plan.SurfaceMode;
    }

    public void SetDirectSizeProvider(Func<(int Width, int Height)> directSizeProvider)
    {
        _directSizeProvider = directSizeProvider;
    }

    public (int Width, int Height) ResolveRenderSize(bool directSuppressed)
    {
        if (DirectSurfaceActive && !directSuppressed && _directSizeProvider is not null)
        {
            return Normalize(_directSizeProvider());
        }

        return Normalize(_fallbackSizeProvider());
    }

    private static (int Width, int Height) Normalize((int Width, int Height) size)
    {
        return (
            NumericMath.AtLeast(size.Width, 1),
            NumericMath.AtLeast(size.Height, 1));
    }
}
