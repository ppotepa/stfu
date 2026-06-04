using STFU.NPR.Graph;

namespace STFU.NPR.Composition;

public sealed record StyleStrokeRule(
    IReadOnlyList<StyleStrokeProfile> Profiles,
    float ThicknessScale,
    float OpacityScale)
{
    public StyleStrokeProfile? FindProfile(NprStrokeIntent intent)
    {
        return Profiles.FirstOrDefault(profile => profile.Intent == intent);
    }

    public StyleStrokeProfile? FindProfile(FeatureCurveKind kind, NprStrokeIntent intent, string? layerName)
    {
        return Profiles
            .Where(profile => profile.Intent == intent)
            .Where(profile => profile.Kind is null || profile.Kind == kind)
            .Where(profile => string.IsNullOrWhiteSpace(profile.LayerName) ||
                string.Equals(profile.LayerName, layerName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(profile => (profile.Kind is null ? 0 : 2) + (string.IsNullOrWhiteSpace(profile.LayerName) ? 0 : 1))
            .FirstOrDefault();
    }
}
