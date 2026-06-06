cbuffer EdgeSampleConstants : register(b0)
{
    uint Width;
    uint Height;
    uint SampleCount;
    uint Padding0;
};

struct EdgeSampleRequest
{
    float2 Pixel;
    uint FirstFaceId;
    uint SecondFaceId;
};

StructuredBuffer<EdgeSampleRequest> Samples : register(t0);
Texture2D<uint> FaceIds : register(t1);
RWStructuredBuffer<uint> Results : register(u0);

bool MatchesAllowed(uint faceId, uint firstFaceId, uint secondFaceId)
{
    return (firstFaceId != 0 && faceId == firstFaceId) ||
           (secondFaceId != 0 && faceId == secondFaceId);
}

[numthreads(64, 1, 1)]
void CS(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint index = dispatchThreadId.x;
    if (index >= SampleCount)
    {
        return;
    }

    EdgeSampleRequest sample = Samples[index];
    int2 center = int2(
        clamp((int)floor(sample.Pixel.x + 0.5f), 0, (int)Width - 1),
        clamp((int)floor(sample.Pixel.y + 0.5f), 0, (int)Height - 1));

    uint visible = 0;
    [unroll]
    for (int dy = -1; dy <= 1; dy++)
    {
        [unroll]
        for (int dx = -1; dx <= 1; dx++)
        {
            int2 p = center + int2(dx, dy);
            if (p.x < 0 || p.y < 0 || p.x >= (int)Width || p.y >= (int)Height)
            {
                continue;
            }

            uint faceId = FaceIds.Load(int3(p, 0));
            if (MatchesAllowed(faceId, sample.FirstFaceId, sample.SecondFaceId))
            {
                visible = 1;
            }
        }
    }

    Results[index] = visible;
}
