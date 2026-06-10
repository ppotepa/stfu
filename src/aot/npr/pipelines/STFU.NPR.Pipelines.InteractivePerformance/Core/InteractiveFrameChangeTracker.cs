using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed class InteractiveFrameChangeTracker
{
    private InteractiveFrameSignature _previousSignature;
    private int _previousWidth;
    private int _previousHeight;
    private InteractiveQualityMode _previousQualityMode;
    private bool _hasPrevious;

    public InteractiveFrameIntent Resolve(InteractiveFrameIntent intent)
    {
        var signature = intent.Signature;
        var firstFrame = !_hasPrevious;

        var viewportChanged = firstFrame ||
            intent.Width != _previousWidth ||
            intent.Height != _previousHeight ||
            signature.ViewportHash != _previousSignature.ViewportHash;
        var sceneChanged = firstFrame ||
            signature.ContentHash != _previousSignature.ContentHash;
        var cameraChanged = firstFrame ||
            signature.CameraHash != _previousSignature.CameraHash ||
            viewportChanged;
        var styleChanged = firstFrame ||
            signature.StyleHash != _previousSignature.StyleHash ||
            intent.QualityMode != _previousQualityMode;
        var debugOverlayChanged = _hasPrevious &&
            signature.DebugHash != _previousSignature.DebugHash;

        _previousSignature = signature;
        _previousWidth = intent.Width;
        _previousHeight = intent.Height;
        _previousQualityMode = intent.QualityMode;
        _hasPrevious = true;

        return intent with
        {
            CameraChanged = cameraChanged,
            SceneChanged = sceneChanged,
            StyleChanged = styleChanged,
            ViewportSizeChanged = viewportChanged,
            DebugOverlayChanged = debugOverlayChanged
        };
    }
}
