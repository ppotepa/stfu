using STFU.Common.Primitives;

namespace STFU.NPR.Analysis;

public readonly record struct FrameProjectionCacheKey(
    MeshHandle Mesh,
    ulong MeshSignature,
    ulong TransformSignature,
    ulong CameraSignature,
    int Width,
    int Height,
    float DepthScale);
