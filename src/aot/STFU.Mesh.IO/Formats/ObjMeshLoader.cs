using STFU.Abstractions.Loading;
using STFU.Mesh;
using STFU.Mesh.Loading;

namespace STFU.MeshIO.Formats;

public sealed class ObjMeshLoader : IMeshLoader<string>
{
    public LoadResult<Mesh.MeshData> Load(string source, LoadContext context)
    {
        if (!File.Exists(source))
        {
            return LoadResult<MeshData>.Fail($"OBJ file was not found: {source}");
        }

        var positions = new List<System.Numerics.Vector3>();
        var triangles = new List<MeshTriangle>();

        foreach (var rawLine in File.ReadLines(source))
        {
            var line = rawLine.Trim();

            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            if (line.StartsWith("v ", StringComparison.Ordinal))
            {
                if (TryParseVertex(line, out var position))
                {
                    positions.Add(position);
                }

                continue;
            }

            if (line.StartsWith("f ", StringComparison.Ordinal))
            {
                ParseFace(line, positions.Count, triangles);
            }
        }

        var vertices = new MeshVertex[positions.Count];
        for (var index = 0; index < positions.Count; index++)
        {
            vertices[index] = new MeshVertex(positions[index], System.Numerics.Vector3.Zero);
        }

        return LoadResult<MeshData>.Ok(new MeshData(vertices, triangles));
    }

    private static bool TryParseVertex(string line, out System.Numerics.Vector3 position)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            position = default;
            return false;
        }

        position = new System.Numerics.Vector3(
            ParseFloat(parts[1]),
            ParseFloat(parts[2]),
            ParseFloat(parts[3]));
        return true;
    }

    private static void ParseFace(
        string line,
        int vertexCount,
        List<MeshTriangle> triangles)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            return;
        }

        Span<int> indices = stackalloc int[parts.Length - 1];
        var count = 0;

        for (var index = 1; index < parts.Length; index++)
        {
            if (TryParseVertexIndex(parts[index], vertexCount, out var vertexIndex))
            {
                indices[count++] = vertexIndex;
            }
        }

        for (var index = 1; index < count - 1; index++)
        {
            triangles.Add(new MeshTriangle(indices[0], indices[index], indices[index + 1]));
        }
    }

    private static bool TryParseVertexIndex(
        string token,
        int vertexCount,
        out int index)
    {
        var slash = token.IndexOf('/');
        var value = slash >= 0 ? token[..slash] : token;

        if (!int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            index = default;
            return false;
        }

        index = parsed > 0 ? parsed - 1 : vertexCount + parsed;
        return index >= 0 && index < vertexCount;
    }

    private static float ParseFloat(string value)
    {
        return float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
}
