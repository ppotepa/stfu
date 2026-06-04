namespace STFU.Rendering.DirectX.Device;

internal static class DirectXResourceId
{
    private static long _next;

    public static long Next()
    {
        return Interlocked.Increment(ref _next);
    }
}
