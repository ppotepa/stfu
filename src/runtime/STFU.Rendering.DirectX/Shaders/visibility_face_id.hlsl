cbuffer VisibilityFrameConstants : register(b0)
{
    float2 BufferSize;
    float2 InvBufferSize;
};

struct VisibilityTriangle
{
    float4 A;
    float4 B;
    float4 C;
};

StructuredBuffer<VisibilityTriangle> Triangles : register(t0);

struct VsOut
{
    float4 Position : SV_Position;
    nointerpolation uint FaceId : TEXCOORD0;
};

float2 ToClip(float2 p)
{
    float2 ndc;
    ndc.x = p.x * InvBufferSize.x * 2.0f - 1.0f;
    ndc.y = 1.0f - p.y * InvBufferSize.y * 2.0f;
    return ndc;
}

VsOut VS(uint vertexId : SV_VertexID)
{
    uint triangleIndex = vertexId / 3;
    uint corner = vertexId - triangleIndex * 3;
    VisibilityTriangle tri = Triangles[triangleIndex];

    float4 vertex = tri.A;
    if (corner == 1)
    {
        vertex = tri.B;
    }
    else if (corner == 2)
    {
        vertex = tri.C;
    }

    VsOut output;
    output.Position = float4(ToClip(vertex.xy), saturate(vertex.z), 1.0f);
    output.FaceId = (uint)vertex.w;
    return output;
}

uint PS(VsOut input) : SV_Target
{
    return input.FaceId;
}
