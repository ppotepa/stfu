namespace STFU.Animation.Skinning;

public readonly record struct VertexSkinWeights(
    int Bone0,
    int Bone1,
    int Bone2,
    int Bone3,
    float Weight0,
    float Weight1,
    float Weight2,
    float Weight3)
{
    public static VertexSkinWeights Empty { get; } = new(-1, -1, -1, -1, 0, 0, 0, 0);
}
