namespace STFU.NPR.Debug;

public sealed class NprDebugState
{
    public NprDebugFrame CurrentFrame { get; private set; } = NprDebugFrame.Empty;

    public void Publish(NprDebugFrame frame)
    {
        CurrentFrame = frame;
    }
}
