namespace STFU.Animation.Skeleton;

public sealed record SkeletonData(IReadOnlyList<BoneData> Bones)
{
    public static SkeletonData Empty { get; } = new([]);

    public int BoneCount => Bones.Count;

    public bool TryGetBoneIndex(string name, out int index)
    {
        for (var i = 0; i < Bones.Count; i++)
        {
            if (string.Equals(Bones[i].Name, name, StringComparison.Ordinal))
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }
}
