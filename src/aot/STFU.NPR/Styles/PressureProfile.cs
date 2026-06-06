using STFU.Common.Math;

namespace STFU.NPR.Styles;

public sealed record PressureProfile(
    float StartPressure,
    float MidPressure,
    float EndPressure)
{
    public float Sample(float t)
    {
        return StrokeMath.PressureSample(StartPressure, MidPressure, EndPressure, t);
    }
}
