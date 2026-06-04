using STFU.NPR.Graph;

namespace STFU.NPR.Styles;

public sealed record MediumProfile(
    StrokeMedium Medium,
    StrokeNoiseProfile Noise,
    PressureProfile Pressure,
    float OpacityFloor,
    float OvershootScale)
{
    public static MediumProfile For(StrokeMedium medium)
    {
        return medium switch
        {
            StrokeMedium.Pencil => new MediumProfile(
                medium,
                new StrokeNoiseProfile(1.15f, 0.45f, 1.35f, 0.65f, 0.12f),
                new PressureProfile(0.72f, 1.0f, 0.76f),
                0.08f,
                0.95f),
            StrokeMedium.Charcoal => new MediumProfile(
                medium,
                new StrokeNoiseProfile(1.35f, 0.55f, 1.5f, 0.8f, 0.18f),
                new PressureProfile(0.84f, 1.08f, 0.82f),
                0.1f,
                1.05f),
            StrokeMedium.Marker => new MediumProfile(
                medium,
                new StrokeNoiseProfile(0.6f, 0.2f, 0.45f, 0.28f, 0.05f),
                new PressureProfile(0.9f, 1.0f, 0.9f),
                0.16f,
                0.45f),
            StrokeMedium.Wash => new MediumProfile(
                medium,
                new StrokeNoiseProfile(0.2f, 0.08f, 0.18f, 0.12f, 0.1f),
                new PressureProfile(0.94f, 1.0f, 0.94f),
                0.14f,
                0.15f),
            _ => new MediumProfile(
                medium,
                new StrokeNoiseProfile(1.0f, 0.35f, 1.0f, 1.0f, 0.08f),
                new PressureProfile(0.82f, 1.0f, 0.84f),
                0.1f,
                1.0f)
        };
    }
}
