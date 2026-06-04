using STFU.Common.Primitives;

namespace STFU.NPR.Graph;

public sealed record MaterialRegion(
    int StableId,
    EntityId EntityId,
    int MaterialId,
    IReadOnlyList<int> TriangleIndices,
    float BaseTone,
    StrokeMedium PreferredMedium,
    RegionHatchingPolicy HatchingPolicy);
