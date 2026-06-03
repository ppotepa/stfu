using System.Runtime.InteropServices;

namespace STFU.Import.Fbx;

internal sealed class NativeFbxSceneHandle : SafeHandle
{
    private NativeFbxSceneHandle()
        : base(IntPtr.Zero, true)
    {
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    public static NativeFbxSceneHandle FromRaw(nint raw)
    {
        var handle = new NativeFbxSceneHandle();
        handle.SetHandle(raw);
        return handle;
    }

    protected override bool ReleaseHandle()
    {
        FbxNative.Free(handle);
        return true;
    }
}
