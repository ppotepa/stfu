namespace STFU.Import.Fbx;

public sealed record FbxImportOptions(
    int AnimationIndex,
    double TimeSeconds)
{
    public static FbxImportOptions BindPose { get; } = new(-1, 0);
}
