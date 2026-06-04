Texture2D ToneTexture : register(t0);
SamplerState LinearSampler : register(s0);

cbuffer FrameConstants : register(b0)
{
    float2 ViewportSize;
    float2 InvViewportSize;
    float Opacity;
    float3 Padding0;
};

struct VsOut
{
    float4 Position : SV_Position;
    float2 Uv : TEXCOORD0;
};

VsOut VS(uint vertexId : SV_VertexID)
{
    float2 pos[3] = {
        float2(-1.0f, -1.0f),
        float2(-1.0f,  3.0f),
        float2( 3.0f, -1.0f)
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
    float4 c = ToneTexture.Sample(LinearSampler, input.Uv);
    float a = c.a * Opacity;
    return float4(c.rgb * a, a);
}
