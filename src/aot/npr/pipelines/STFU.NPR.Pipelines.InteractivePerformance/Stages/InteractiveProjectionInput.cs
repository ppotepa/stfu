using STFU.Camera;
using STFU.NPR.Settings;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

internal sealed record InteractiveProjectionInput(
    IReadOnlyList<InteractiveProjectionInputMesh> Meshes,
    CameraState Camera,
    NprSettings Settings,
    int Width,
    int Height,
    int FrameId,
    float TimeSeconds,
    string SourceNote)
{
    public int MeshCount => Meshes.Count;

    public int VertexCount
    {
        get
        {
            var total = 0;
            for (var i = 0; i < Meshes.Count; i++)
            {
                total += Meshes[i].VertexCount;
            }

            return total;
        }
    }

    public int TriangleCount
    {
        get
        {
            var total = 0;
            for (var i = 0; i < Meshes.Count; i++)
            {
                total += Meshes[i].TriangleCount;
            }

            return total;
        }
    }

    public bool HasGeometry => VertexCount > 0 || TriangleCount > 0;

    public static InteractiveProjectionInput Empty(
        CameraState camera,
        NprSettings settings,
        int width,
        int height,
        int frameId,
        float timeSeconds,
        string sourceNote)
    {
        return new InteractiveProjectionInput(
            Array.Empty<InteractiveProjectionInputMesh>(),
            camera,
            settings,
            width,
            height,
            frameId,
            timeSeconds,
            sourceNote);
    }
}
