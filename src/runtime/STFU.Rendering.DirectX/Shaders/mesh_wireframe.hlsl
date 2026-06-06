cbuffer MeshFrameConstants : register(b0)
{
    float2 ViewportSize;
    float2 InvViewportSize;
    float CoverageSoftness;
    float3 Padding0;
    float4 StrokeColor;
};

struct MeshVertex
{
    float4 Position;
};

struct MeshEdge
{
    uint Start;
    uint End;
};

StructuredBuffer<MeshVertex> Vertices : register(t0);
StructuredBuffer<MeshEdge> Edges : register(t1);

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

VsOut VS(uint vertexId : SV_VertexID, uint edgeId : SV_InstanceID)
{
    MeshEdge edge = Edges[edgeId];
    float2 p0 = Vertices[edge.Start].Position.xy;
    float2 p1 = Vertices[edge.End].Position.xy;

    float2 corners[6] = {
        float2(-1, 0),
        float2( 1, 0),
        float2(-1, 1),
        float2(-1, 1),
        float2( 1, 0),
        float2( 1, 1)
    };

    float thickness = max(0.35f, StrokeColor.w);
    float softness = CoverageSoftness;
    float2 dir = p1 - p0;
    float len = max(length(dir), 0.0001f);
    dir /= len;
    float2 n = float2(-dir.y, dir.x);

    float side = corners[vertexId].x;
    float t = corners[vertexId].y;
    float halfWidth = thickness * 0.5f + softness + 1.0f;
    float cap = halfWidth;
    float2 basePoint = lerp(p0 - dir * cap, p1 + dir * cap, t);
    float2 pixel = basePoint + n * side * halfWidth;

    VsOut output;
    output.Position = float4(ToClip(pixel), 0.0f, 1.0f);
    output.PixelPos = pixel;
    output.P0 = p0;
    output.P1 = p1;
    output.ColorOpacity = float4(StrokeColor.rgb, 1.0f);
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
    float softness = max(0.25f, CoverageSoftness);
    float halfWidth = input.Thickness * 0.5f;
    float dist = DistanceToSegment(input.PixelPos, input.P0, input.P1);
    float coverage = saturate((halfWidth + softness - dist) / softness);
    float alpha = input.ColorOpacity.a * coverage;
    return float4(input.ColorOpacity.rgb * alpha, alpha);
}
