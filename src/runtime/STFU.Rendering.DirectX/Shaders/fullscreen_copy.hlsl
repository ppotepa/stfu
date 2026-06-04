Texture2D SourceTexture : register(t0);
SamplerState LinearSampler : register(s0);

struct VsOut
{
    float4 Position : SV_Position;
    float2 Uv : TEXCOORD0;
};

VsOut VS(uint vertexId : SV_VertexID)
{
    float2 pos[3] = {
        float2(-1.0f, -1.0f),
        float2(-1.0f, 3.0f),
        float2(3.0f, -1.0f)
    };

    float2 uv[3] = {
        float2(0.0f, 1.0f),
        float2(0.0f, -1.0f),
        float2(2.0f, 1.0f)
    };

    VsOut output;
    output.Position = float4(pos[vertexId], 0.0f, 1.0f);
    output.Uv = uv[vertexId];
    return output;
}

float4 PS(VsOut input) : SV_Target
{
    return SourceTexture.Sample(LinearSampler, input.Uv);
}
