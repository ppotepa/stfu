namespace STFU.Common.Math;

public readonly record struct StrokeHumanizationPass(
    float Alpha,
    float Jitter,
    float WidthMultiplier);

public static class StrokeHumanizationMath
{
    public static int PassCount(string styleName)
    {
        return styleName switch
        {
            "Pencil" => 3,
            "Brush" => 2,
            "ComicInk" => 2,
            _ => 1
        };
    }

    public static StrokeHumanizationPass Pass(string styleName, float baseJitter, int pass)
    {
        return styleName switch
        {
            "Pencil" => new StrokeHumanizationPass(0.18f, baseJitter * (1f + pass * 0.55f), 0.9f),
            "Brush" => new StrokeHumanizationPass(pass == 0 ? 0.28f : 0.75f, baseJitter * (pass == 0 ? 1.1f : 0.35f), pass == 0 ? 1.6f : 0.85f),
            "ComicInk" => new StrokeHumanizationPass(pass == 0 ? 0.98f : 0.34f, baseJitter * (pass == 0 ? 0.16f : 0.46f), pass == 0 ? 1.10f : 0.54f),
            _ => new StrokeHumanizationPass(0.92f, baseJitter * 0.35f, 0.75f)
        };
    }

    public static bool ShouldSkipSegment(string styleName, int index, int seed, int pass, bool fastNoise)
    {
        if (styleName == "Pencil" &&
            NoiseMath.Noise01(index * 2.13d + pass, seed, fastNoise) < 0.06d)
        {
            return true;
        }

        return styleName == "ComicInk" &&
               pass == 1 &&
               NoiseMath.Noise01(index * 3.81d, seed, fastNoise) < 0.26d;
    }

    public static (float X, float Y) JitterPoint(
        float x,
        float y,
        float previousX,
        float previousY,
        float nextX,
        float nextY,
        int index,
        float amount,
        int seed,
        bool fastNoise)
    {
        var tx = nextX - previousX;
        var ty = nextY - previousY;
        var length = Geometry2D.SegmentLength(previousX, previousY, nextX, nextY);
        if (length <= 1e-5f)
        {
            length = 1f;
        }

        var nx = -ty / length;
        var ny = tx / length;
        var noise = NoiseMath.Noise01(index * 5.31d, seed, fastNoise) - 0.5d;
        return (
            x + nx * amount * (float)noise,
            y + ny * amount * (float)noise);
    }

    public static double PressureNoise(double pressure, double t, int seed, int pass, bool comicInk, bool fastNoise)
    {
        return 1d + pressure *
            ((NoiseMath.Noise01(t * 9d + pass * 1.7d, seed, fastNoise) - 0.5d) *
             (comicInk ? 1.25d : 1.7d));
    }

    public static double Taper(double t, bool comicInk)
    {
        return comicInk
            ? 0.82d + 0.30d * NumericMath.Sin(NumericMath.Pi * t)
            : 1d;
    }

    public static float LineWidth(float baseWidth, float widthMultiplier, double pressureNoise, double taper, float minimum = 0.35f)
    {
        return (float)NumericMath.AtLeast(baseWidth * widthMultiplier * pressureNoise * taper, minimum);
    }
}
