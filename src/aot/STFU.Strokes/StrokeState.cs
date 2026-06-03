namespace STFU.Strokes;

public sealed class StrokeState
{
    public StrokeFrame CurrentFrame { get; private set; } = StrokeFrame.Empty;

    public void Publish(StrokeFrame frame)
    {
        CurrentFrame = frame;
    }
}
