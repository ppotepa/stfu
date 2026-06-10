namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public readonly record struct InteractiveFrameSignature(
    ulong ContentHash,
    ulong CameraHash,
    ulong StyleHash,
    ulong ViewportHash,
    ulong DebugHash)
{
    public static InteractiveFrameSignature Empty => default;

    public bool HasAnyHash =>
        ContentHash != 0 ||
        CameraHash != 0 ||
        StyleHash != 0 ||
        ViewportHash != 0 ||
        DebugHash != 0;
}
