using STFU.NPR.Graph;

namespace STFU.NPR.Pipeline.Default.Steps;

public sealed class DefaultBuildFaceIdVisibilityBufferStep : STFU.NPR.Pipeline.INprStep
{
    private DefaultFaceIdVisibilityBuffer? _buffer;

    public void Execute(STFU.NPR.Pipeline.NprContext context)
    {
        var drawing = context.Settings.DefaultDrawing;
        var width = Math.Max(8, (int)MathF.Floor(context.Width * drawing.DepthScale));
        var height = Math.Max(8, (int)MathF.Floor(context.Height * drawing.DepthScale));

        var buffer = RentBuffer(width, height, context.Graph.Triangles.Count);
        context.Graph.DefaultFaceIdVisibility = buffer;

        if (!drawing.OcclusionCulling)
        {
            Array.Fill(buffer.FaceVisible, true);
            return;
        }

        for (var triangleIndex = 0; triangleIndex < context.Graph.Triangles.Count; triangleIndex++)
        {
            var triangle = context.Graph.Triangles[triangleIndex];
            var a = context.Graph.Vertices[triangle.A];
            var b = context.Graph.Vertices[triangle.B];
            var c = context.Graph.Vertices[triangle.C];

            if (TriangleOutsideClip(a.Ndc, b.Ndc, c.Ndc))
            {
                continue;
            }

            Rasterize(context, buffer, triangleIndex, a, b, c);
        }

        buffer.MarkVisibleFaces();
    }

    private DefaultFaceIdVisibilityBuffer RentBuffer(int width, int height, int faceCount)
    {
        if (_buffer is null ||
            _buffer.Width != width ||
            _buffer.Height != height ||
            _buffer.FaceCapacity < faceCount)
        {
            _buffer = new DefaultFaceIdVisibilityBuffer(width, height, faceCount);
        }
        else
        {
            _buffer.Clear();
        }

        return _buffer;
    }

    private static void Rasterize(
        STFU.NPR.Pipeline.NprContext context,
        DefaultFaceIdVisibilityBuffer buffer,
        int triangleIndex,
        ProjectedVertex a,
        ProjectedVertex b,
        ProjectedVertex c)
    {
        var scaleX = buffer.Width / (float)Math.Max(context.Width, 1);
        var scaleY = buffer.Height / (float)Math.Max(context.Height, 1);
        var av = ToBufferVertex(scaleX, scaleY, a);
        var bv = ToBufferVertex(scaleX, scaleY, b);
        var cv = ToBufferVertex(scaleX, scaleY, c);
        var area = DefaultFaceIdVisibilityBuffer.EdgeFunction(av.X, av.Y, bv.X, bv.Y, cv.X, cv.Y);
        if (MathF.Abs(area) < 1e-7f)
        {
            return;
        }

        var minX = Math.Max(0, (int)MathF.Floor(MathF.Min(av.X, MathF.Min(bv.X, cv.X)) - 1f));
        var maxX = Math.Min(buffer.Width - 1, (int)MathF.Ceiling(MathF.Max(av.X, MathF.Max(bv.X, cv.X)) + 1f));
        var minY = Math.Max(0, (int)MathF.Floor(MathF.Min(av.Y, MathF.Min(bv.Y, cv.Y)) - 1f));
        var maxY = Math.Min(buffer.Height - 1, (int)MathF.Ceiling(MathF.Max(av.Y, MathF.Max(bv.Y, cv.Y)) + 1f));

        var stepX0 = cv.Y - bv.Y;
        var stepY0 = -(cv.X - bv.X);
        var stepX1 = av.Y - cv.Y;
        var stepY1 = -(av.X - cv.X);
        var stepX2 = bv.Y - av.Y;
        var stepY2 = -(bv.X - av.X);
        var rowStartX = minX + 0.5f;
        var rowStartY = minY + 0.5f;
        var rowW0 = DefaultFaceIdVisibilityBuffer.EdgeFunction(bv.X, bv.Y, cv.X, cv.Y, rowStartX, rowStartY);
        var rowW1 = DefaultFaceIdVisibilityBuffer.EdgeFunction(cv.X, cv.Y, av.X, av.Y, rowStartX, rowStartY);
        var rowW2 = DefaultFaceIdVisibilityBuffer.EdgeFunction(av.X, av.Y, bv.X, bv.Y, rowStartX, rowStartY);
        var positiveArea = area >= 0f;

        for (var y = minY; y <= maxY; y++)
        {
            var w0 = rowW0;
            var w1 = rowW1;
            var w2 = rowW2;

            for (var x = minX; x <= maxX; x++)
            {
                var inside = positiveArea
                    ? w0 >= -1e-5f && w1 >= -1e-5f && w2 >= -1e-5f
                    : w0 <= 1e-5f && w1 <= 1e-5f && w2 <= 1e-5f;

                if (!inside)
                {
                    w0 += stepX0;
                    w1 += stepX1;
                    w2 += stepX2;
                    continue;
                }

                var l0 = w0 / area;
                var l1 = w1 / area;
                var l2 = w2 / area;
                var depth = l0 * av.Depth01 + l1 * bv.Depth01 + l2 * cv.Depth01;

                if (depth is < 0f or > 1f)
                {
                    w0 += stepX0;
                    w1 += stepX1;
                    w2 += stepX2;
                    continue;
                }

                var index = y * buffer.Width + x;
                if (depth < buffer.Depth[index])
                {
                    buffer.Depth[index] = depth;
                    buffer.FaceId[index] = triangleIndex;
                }

                w0 += stepX0;
                w1 += stepX1;
                w2 += stepX2;
            }

            rowW0 += stepY0;
            rowW1 += stepY1;
            rowW2 += stepY2;
        }
    }

    private static BufferVertex ToBufferVertex(float scaleX, float scaleY, ProjectedVertex vertex)
    {
        return new BufferVertex(
            vertex.Position.X * scaleX,
            vertex.Position.Y * scaleY,
            vertex.Depth01);
    }

    private static bool TriangleOutsideClip(System.Numerics.Vector3 a, System.Numerics.Vector3 b, System.Numerics.Vector3 c)
    {
        if (a.X < -1f && b.X < -1f && c.X < -1f) return true;
        if (a.X > 1f && b.X > 1f && c.X > 1f) return true;
        if (a.Y < -1f && b.Y < -1f && c.Y < -1f) return true;
        if (a.Y > 1f && b.Y > 1f && c.Y > 1f) return true;
        if (a.Z < -1f && b.Z < -1f && c.Z < -1f) return true;
        if (a.Z > 1f && b.Z > 1f && c.Z > 1f) return true;
        return false;
    }

    private readonly record struct BufferVertex(float X, float Y, float Depth01);
}
