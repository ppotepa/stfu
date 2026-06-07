using Avalonia;
using STFU.Common.Math;
using STFU.UI.Bridge.Session;

namespace STFU.UI;

internal sealed class ViewportInputController
{
    private readonly UiEngineSession _session;
    private bool _loggedOrbitInput;
    private bool _loggedPanInput;
    private bool _loggedFovInput;

    public ViewportInputController(UiEngineSession session)
    {
        _session = session;
    }

    public bool MoveCamera(Point delta, bool pan)
    {
        if (NumericMath.Abs(delta.X) < 0.001 && NumericMath.Abs(delta.Y) < 0.001)
        {
            return false;
        }

        if (pan)
        {
            const float panUnitsPerPixel = 0.005f;
            _session.Workspace.Camera.Pan((float)-delta.X * panUnitsPerPixel, (float)delta.Y * panUnitsPerPixel);

            if (!_loggedPanInput)
            {
                StfuUiLog.Write("Viewport pan input active: Ctrl + left mouse drag.");
                _loggedPanInput = true;
            }

            return true;
        }

        const float radiansPerPixel = 0.01f;
        _session.Workspace.Camera.Orbit((float)delta.X * radiansPerPixel, (float)-delta.Y * radiansPerPixel);

        if (!_loggedOrbitInput)
        {
            StfuUiLog.Write("Viewport orbit input active: left mouse drag.");
            _loggedOrbitInput = true;
        }

        return true;
    }

    public bool ZoomCamera(double wheelSteps)
    {
        if (NumericMath.Abs(wheelSteps) < 0.001)
        {
            return false;
        }

        const float degreesPerWheelStep = 3f;
        _session.Workspace.Camera.AdjustFieldOfView((float)-wheelSteps * degreesPerWheelStep);

        if (!_loggedFovInput)
        {
            StfuUiLog.Write("Viewport FOV input active: mouse wheel.");
            _loggedFovInput = true;
        }

        return true;
    }
}
