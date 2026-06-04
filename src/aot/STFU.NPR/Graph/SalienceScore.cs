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
            Geometry = Math.Clamp(score.Geometry, 0f, 1f),
            Visibility = Math.Clamp(score.Visibility, 0f, 1f),
            Tone = Math.Clamp(score.Tone, 0f, 1f),
            Material = Math.Clamp(score.Material, 0f, 1f),
            Style = Math.Clamp(score.Style, 0f, 1f),
            Focus = Math.Clamp(score.Focus, 0f, 1f),
            ClutterPenalty = Math.Clamp(score.ClutterPenalty, 0f, 1f),
            Final = Math.Clamp(score.Final, 0f, 1f)
        };
    }
}
