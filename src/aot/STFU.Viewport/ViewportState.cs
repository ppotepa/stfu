using STFU.Viewport.Snapshots;

namespace STFU.Viewport;

public sealed class ViewportState
{
    public int Width { get; private set; } = 1280;

    public int Height { get; private set; } = 720;

    public ViewportRenderMode RenderMode { get; private set; } = ViewportRenderMode.Mesh;

    public ViewportSnapshot Snapshot { get; private set; } = new(
        1280,
        720,
        ViewportRenderMode.Mesh,
        Strokes.StrokeFrame.Empty);

    public void Resize(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
    }

    public void SetRenderMode(ViewportRenderMode renderMode)
    {
        RenderMode = renderMode;
    }

    public void Publish(ViewportSnapshot snapshot)
    {
        Snapshot = snapshot;
    }
}
