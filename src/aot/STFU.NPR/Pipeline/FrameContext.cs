using STFU.Camera;
using STFU.NPR.Composition;
using STFU.NPR.Settings;
using STFU.NPR.Temporal;

namespace STFU.NPR.Pipeline;

public sealed record FrameContext(
    CameraState Camera,
    ProjectionInfo Projection,
    LightContext Lighting,
    NprSettings Settings,
    StyleGrammar Style,
    string ActivePresetId,
    int FrameId,
    float TimeSeconds,
    FrameHistory? PreviousFrame)
{
    public static FrameContext From(NprViewContext view)
    {
        return new FrameContext(
            view.Camera,
            view.Projection,
            view.Lighting,
            view.Settings,
            view.Style,
            view.ActivePresetId,
            view.FrameId,
            view.TimeSeconds,
            view.PreviousFrame);
    }
}
