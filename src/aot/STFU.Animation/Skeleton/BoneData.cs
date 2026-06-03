using System.Numerics;

namespace STFU.Animation.Skeleton;

public sealed record BoneData(
    int Index,
    string Name,
    int ParentIndex,
    Matrix4x4 InverseBindMatrix)
{
    public bool IsRoot => ParentIndex < 0;
}
