namespace STFU.Common.Math;

public static class SignatureMath
{
    public static ulong StartFnv1A() => HashMath.FnvOffset64;

    public static ulong Add(ulong hash, int value) => HashMath.Fnv1A(hash, value);

    public static ulong Add(ulong hash, uint value) => HashMath.Fnv1A(hash, value);

    public static ulong Add(ulong hash, long value) => HashMath.Fnv1A(hash, value);

    public static ulong Add(ulong hash, ulong value) => HashMath.Fnv1A(hash, value);

    public static ulong Add(ulong hash, float value) => HashMath.Fnv1A(hash, value);

    public static ulong Add(ulong hash, double value) => HashMath.Fnv1A(hash, value);

    public static ulong Add(ulong hash, string? value) => HashMath.Fnv1A(hash, value);
}
