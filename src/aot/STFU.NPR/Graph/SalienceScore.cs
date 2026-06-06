using STFU.Common.Math;

namespace STFU.NPR.Graph;

public readonly record struct SalienceScore(
    float Geometry,
    float Visibility,
    float Tone,
    float Material,
    float Style,
    float Focus,
    float ClutterPenalty,
    float Final)
{
    public static SalienceScore Clamp(SalienceScore score)
    {
        return score with
        {
            Geometry = NumericMath.Clamp01(score.Geometry),
            Visibility = NumericMath.Clamp01(score.Visibility),
            Tone = NumericMath.Clamp01(score.Tone),
            Material = NumericMath.Clamp01(score.Material),
            Style = NumericMath.Clamp01(score.Style),
            Focus = NumericMath.Clamp01(score.Focus),
            ClutterPenalty = NumericMath.Clamp01(score.ClutterPenalty),
            Final = NumericMath.Clamp01(score.Final)
        };
    }
}
