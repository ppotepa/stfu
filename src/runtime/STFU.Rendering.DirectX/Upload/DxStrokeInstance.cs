using System.Numerics;
using System.Runtime.InteropServices;

namespace STFU.Rendering.DirectX.Upload;

[StructLayout(LayoutKind.Sequential)]
public readonly struct DxStrokeInstance
{
    public readonly Vector4 P0P1;
    public readonly Vector4 ColorOpacity;
    public readonly Vector4 ThicknessOrderFlags;

    public DxStrokeInstance(
        float x0,
        float y0,
        float x1,
        float y1,
        float r,
        float g,
        float b,
        float opacity,
        float thickness,
        float order,
        float flags)
    {
        P0P1 = new Vector4(x0, y0, x1, y1);
        ColorOpacity = new Vector4(r, g, b, opacity);
        ThicknessOrderFlags = new Vector4(thickness, order, flags, 0f);
    }

    public const int SizeInBytes = 48;
}
