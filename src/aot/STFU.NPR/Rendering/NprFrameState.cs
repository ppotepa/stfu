namespace STFU.NPR.Rendering;

public sealed class NprFrameState
{
    public NprFrame CurrentFrame { get; private set; } = NprFrame.Empty;

    public void Publish(NprFrame frame)
    {
        CurrentFrame = frame;
    }
}
