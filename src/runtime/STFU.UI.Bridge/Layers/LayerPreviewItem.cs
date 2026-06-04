namespace STFU.UI.Bridge.Layers;

public sealed record LayerPreviewItem(
    string Type,
    double X,
    double Y,
    double Width,
    double Height,
    double Opacity,
    string Color,
    double Rotation);
