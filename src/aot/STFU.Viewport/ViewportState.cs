using STFU.Viewport.Snapshots;

namespace STFU.Viewport;

public sealed class ViewportState
{
    public int Width { get; private set; } = 1280;

    public int Height { get; private set; } = 720;

    public ViewportSnapshot Snapshot { get; private set; } = new(1280, 720, Strokes.StrokeFrame.Empty);

    public void Resize(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
    }

    public void Publish(ViewportSnapshot snapshot)
    {
        Snapshot = snapshot;
    }
}
