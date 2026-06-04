namespace STFU.NPR.Composition;

public readonly record struct PresetVersion(
    int Major,
    int Minor,
    int Patch)
{
    public override string ToString()
    {
        return $"{Major}.{Minor}.{Patch}";
    }
}
