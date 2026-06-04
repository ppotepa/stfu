cbuffer FrameConstants : register(b0)
{
    float2 ViewportSize;
    float2 InvViewportSize;
    float CoverageSoftness;
    float3 Padding0;
};

struct StrokeInstance
{
    float4 P0P1;
    float4 ColorOpacity;
    float4 ThicknessOrderFlags;
};

StructuredBuffer<StrokeInstance> Strokes : register(t0);

struct VsOut
{
    float4 Position : SV_Position;
    float2 PixelPos : TEXCOORD0;
    float2 P0 : TEXCOORD1;
    float2 P1 : TEXCOORD2;
    float4 ColorOpacity : COLOR0;
    float Thickness : TEXCOORD3;
};

float2 ToClip(float2 p)
{
    float2 ndc;
    ndc.x = p.x * InvViewportSize.x * 2.0f - 1.0f;
    ndc.y = 1.0f - p.y * InvViewportSize.y * 2.0f;
    return ndc;
}

VsOut VS(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    StrokeInstance s = Strokes[instanceId];
    float2 p0 = s.P0P1.xy;
    float2 p1 = s.P0P1.zw;
    float thickness = max(0.35f, s.ThicknessOrderFlags.x);

    float2 dir = p1 - p0;
    float len = max(length(dir), 0.0001f);
    dir /= len;
    float2 n = float2(-dir.y, dir.x);

    float2 corners[6] = {
        float2(-1, 0),
        float2( 1, 0),
        float2(-1, 1),
        float2(-1, 1),
        float2( 1, 0),
        float2( 1, 1)
    };

    float side = corners[vertexId].x;
    float t = corners[vertexId].y;
    float halfWidth = thickness * 0.5f + CoverageSoftness + 1.0f;
    float cap = halfWidth;
    float2 basePoint = lerp(p0 - dir * cap, p1 + dir * cap, t);
    float2 pixel = basePoint + n * side * halfWidth;

    VsOut output;
    output.Position = float4(ToClip(pixel), 0.0f, 1.0f);
    output.PixelPos = pixel;
    output.P0 = p0;
    output.P1 = p1;
    output.ColorOpacity = s.ColorOpacity;
    output.Thickness = thickness;
    return output;
}

float DistanceToSegment(float2 p, float2 a, float2 b)
{
    float2 ab = b - a;
    float denom = max(dot(ab, ab), 0.0001f);
    float t = saturate(dot(p - a, ab) / denom);
    float2 c = a + ab * t;
    return length(p - c);
}

float4 PS(VsOut input) : SV_Target
{
    float halfWidth = input.Thickness * 0.5f;
    float dist = DistanceToSegment(input.PixelPos, input.P0, input.P1);
    float softness = max(0.25f, CoverageSoftness);
    float coverage = saturate((halfWidth + softness - dist) / softness);
    float alpha = input.ColorOpacity.a * coverage;
    float3 rgb = input.ColorOpacity.rgb;
    return float4(rgb * alpha, alpha);
}
