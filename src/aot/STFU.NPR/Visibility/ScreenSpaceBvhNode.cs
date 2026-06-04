namespace STFU.NPR.Visibility;

internal sealed record ScreenSpaceBvhNode(
    ScreenSpaceBounds Bounds,
    int Start,
    int Count,
    ScreenSpaceBvhNode? Left,
    ScreenSpaceBvhNode? Right)
{
    public bool IsLeaf => Left is null && Right is null;
}
