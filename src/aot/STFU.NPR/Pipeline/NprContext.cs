using STFU.Assets;
using STFU.Camera;
using STFU.Engine.Scenes;
using STFU.NPR.Graph;
using STFU.NPR.Settings;
using STFU.Strokes;

namespace STFU.NPR.Pipeline;

public sealed class NprContext
{
    public required Scene Scene { get; init; }

    public required AssetRegistry Assets { get; init; }

    public required CameraState Camera { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    public required NprSettings Settings { get; init; }

    public NprGraph Graph { get; } = new();

    public StrokeFrame Frame { get; set; } = StrokeFrame.Empty;
}
