cbuffer VisibilityReduceConstants : register(b0)
{
    uint Width;
    uint Height;
    uint FaceCount;
    uint Padding0;
};

Texture2D<uint> FaceIds : register(t0);
RWStructuredBuffer<uint> VisibleWords : register(u0);

[numthreads(8, 8, 1)]
void CS(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    if (dispatchThreadId.x >= Width || dispatchThreadId.y >= Height)
    {
        return;
    }

    uint faceId = FaceIds.Load(int3(dispatchThreadId.xy, 0));
    if (faceId == 0)
    {
        return;
    }

    uint faceIndex = faceId - 1;
    if (faceIndex >= FaceCount)
    {
        return;
    }

    uint wordIndex = faceIndex >> 5;
    uint mask = 1u << (faceIndex & 31u);
    InterlockedOr(VisibleWords[wordIndex], mask);
}
