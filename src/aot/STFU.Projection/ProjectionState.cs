namespace STFU.Projection;

public sealed class ProjectionState
{
    public CameraState Camera { get; private set; } = CameraState.Default;

    public void SetCamera(CameraState camera)
    {
        Camera = camera;
    }
}
