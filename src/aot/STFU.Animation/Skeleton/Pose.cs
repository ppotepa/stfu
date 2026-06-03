using System.Numerics;

namespace STFU.Animation.Skeleton;

public sealed record Pose(
    IReadOnlyList<Matrix4x4> LocalTransforms,
    IReadOnlyList<Matrix4x4> ModelTransforms)
{
    public static Pose Identity(SkeletonData skeleton)
    {
        var local = new Matrix4x4[skeleton.BoneCount];
        var model = new Matrix4x4[skeleton.BoneCount];

        Array.Fill(local, Matrix4x4.Identity);
        Array.Fill(model, Matrix4x4.Identity);

        return new Pose(local, model);
    }
}
