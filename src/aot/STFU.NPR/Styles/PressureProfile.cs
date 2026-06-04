namespace STFU.NPR.Styles;

public sealed record PressureProfile(
    float StartPressure,
    float MidPressure,
    float EndPressure)
{
    public float Sample(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        if (t <= 0.5f)
        {
            var local = t / 0.5f;
            return Lerp(StartPressure, MidPressure, local);
        }

        return Lerp(MidPressure, EndPressure, (t - 0.5f) / 0.5f);
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
}
