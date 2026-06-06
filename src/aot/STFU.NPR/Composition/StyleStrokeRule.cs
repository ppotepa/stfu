using STFU.NPR.Graph;

namespace STFU.NPR.Composition;

public sealed record StyleStrokeRule(
    IReadOnlyList<StyleStrokeProfile> Profiles,
    float ThicknessScale,
    float OpacityScale)
{
    public StyleStrokeProfile? FindProfile(NprStrokeIntent intent)
    {
        for (var i = 0; i < Profiles.Count; i++)
        {
            if (Profiles[i].Intent == intent)
            {
                return Profiles[i];
            }
        }

        return null;
    }

    public StyleStrokeProfile? FindProfile(FeatureCurveKind kind, NprStrokeIntent intent, string? layerName)
    {
        StyleStrokeProfile? best = null;
        var bestScore = int.MinValue;

        for (var i = 0; i < Profiles.Count; i++)
        {
            var profile = Profiles[i];
            if (profile.Intent != intent)
            {
                continue;
            }

            if (profile.Kind is not null && profile.Kind != kind)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(profile.LayerName) &&
                !string.Equals(profile.LayerName, layerName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var score = (profile.Kind is null ? 0 : 2) +
                (string.IsNullOrWhiteSpace(profile.LayerName) ? 0 : 1);
            if (score > bestScore)
            {
                best = profile;
                bestScore = score;
            }
        }

        return best;
    }
}
