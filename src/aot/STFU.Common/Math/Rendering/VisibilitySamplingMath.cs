using System.Numerics;

namespace STFU.Common.Math;

public static class VisibilitySamplingMath
{
    public static int ToBufferCoordinate(float screenCoordinate, int viewportPixels, int bufferPixels)
    {
        return RasterMath.ToBufferCoordinate(screenCoordinate, viewportPixels, bufferPixels);
    }

    public static float EdgeFunction(float ax, float ay, float bx, float by, float px, float py)
    {
        return RasterMath.EdgeFunction(ax, ay, bx, by, px, py);
    }

    public static bool TriangleOutsideClip(Vector3 a, Vector3 b, Vector3 c)
    {
        return Geometry3D.TriangleOutsideClip(a, b, c);
    }
}
