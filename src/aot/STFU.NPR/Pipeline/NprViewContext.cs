using STFU.Camera;
using STFU.NPR.Composition;
using STFU.NPR.Settings;
using STFU.NPR.Temporal;
using STFU.Strokes;

namespace STFU.NPR.Pipeline;

public sealed record NprViewContext(
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
    public int Width => Projection.Width;

    public int Height => Projection.Height;

    public Point2D ScreenCenter => new(Width * 0.5f, Height * 0.5f);

    public static NprViewContext Create(
        CameraState camera,
        int width,
        int height,
        NprSettings settings,
        StyleGrammar style,
        LightContext? lighting = null)
    {
        return new NprViewContext(
            camera,
            ProjectionInfo.Create(camera, width, height, settings),
            lighting ?? LightContext.Default,
            settings,
            style,
            style.StyleId,
            0,
            0f,
            null);
    }

    public NprViewContext WithoutHistory()
    {
        return this with { PreviousFrame = null };
    }
}
