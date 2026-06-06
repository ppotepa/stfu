using System.Globalization;
using System.Numerics;
using STFU.Abstractions.Loading;
using STFU.Common.Math;
using STFU.Mesh;
using STFU.Mesh.Loading;

namespace STFU.MeshIO.Formats;

public sealed class ObjMeshLoader : IMeshLoader<string>
{
    public LoadResult<MeshData> Load(string source, LoadContext context)
    {
        if (!File.Exists(source))
        {
            return LoadResult<MeshData>.Fail($"OBJ file was not found: {source}");
        }

        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var vertices = new List<MeshVertex>();
        var generatedNormalSums = new List<Vector3>();
        var vertexMap = new Dictionary<ObjVertexKey, int>();
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
                if (TryParseVector3(line, out var position))
                {
                    positions.Add(position);
                }

                continue;
            }

            if (line.StartsWith("vn ", StringComparison.Ordinal))
            {
                if (TryParseVector3(line, out var normal))
                {
                    normals.Add(Geometry3D.NormalizeOrDefault(normal, Vector3.Zero));
                }

                continue;
            }

            if (line.StartsWith("f ", StringComparison.Ordinal))
            {
                ParseFace(line, positions, normals, vertices, generatedNormalSums, vertexMap, triangles);
            }
        }

        NormalizeGeneratedNormals(vertices, generatedNormalSums);
        return LoadResult<MeshData>.Ok(new MeshData(vertices, triangles));
    }

    private static bool TryParseVector3(string line, out Vector3 value)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            value = default;
            return false;
        }

        value = new Vector3(
            ParseFloat(parts[1]),
            ParseFloat(parts[2]),
            ParseFloat(parts[3]));
        return true;
    }

    private static void ParseFace(
        string line,
        IReadOnlyList<Vector3> positions,
        IReadOnlyList<Vector3> normals,
        List<MeshVertex> vertices,
        List<Vector3> generatedNormalSums,
        Dictionary<ObjVertexKey, int> vertexMap,
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
            if (TryParseFaceVertex(parts[index], positions.Count, normals.Count, out var vertexRef))
            {
                indices[count++] = GetOrCreateVertex(vertexRef, positions, normals, vertices, generatedNormalSums, vertexMap);
            }
        }

        for (var index = 1; index < count - 1; index++)
        {
            var triangle = new MeshTriangle(indices[0], indices[index], indices[index + 1]);
            triangles.Add(triangle);
            AccumulateGeneratedNormal(vertices, generatedNormalSums, triangle);
        }
    }

    private static int GetOrCreateVertex(
        ObjVertexRef vertexRef,
        IReadOnlyList<Vector3> positions,
        IReadOnlyList<Vector3> normals,
        List<MeshVertex> vertices,
        List<Vector3> generatedNormalSums,
        Dictionary<ObjVertexKey, int> vertexMap)
    {
        var key = new ObjVertexKey(vertexRef.PositionIndex, vertexRef.NormalIndex);
        if (vertexMap.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var normal = vertexRef.NormalIndex >= 0
            ? normals[vertexRef.NormalIndex]
            : Vector3.Zero;
        var index = vertices.Count;
        vertices.Add(new MeshVertex(positions[vertexRef.PositionIndex], normal));
        generatedNormalSums.Add(Vector3.Zero);
        vertexMap.Add(key, index);
        return index;
    }

    private static bool TryParseFaceVertex(
        string token,
        int positionCount,
        int normalCount,
        out ObjVertexRef vertexRef)
    {
        vertexRef = default;
        var firstSlash = token.IndexOf('/');
        var positionValue = firstSlash >= 0 ? token[..firstSlash] : token;

        if (!TryParseObjIndex(positionValue, positionCount, out var positionIndex))
        {
            return false;
        }

        var normalIndex = -1;
        if (firstSlash >= 0 && normalCount > 0)
        {
            var secondSlash = token.IndexOf('/', firstSlash + 1);
            if (secondSlash >= 0 && secondSlash < token.Length - 1)
            {
                if (TryParseObjIndex(token[(secondSlash + 1)..], normalCount, out var parsedNormalIndex))
                {
                    normalIndex = parsedNormalIndex;
                }
            }
        }

        vertexRef = new ObjVertexRef(positionIndex, normalIndex);
        return true;
    }

    private static bool TryParseObjIndex(string value, int count, out int index)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            index = default;
            return false;
        }

        index = parsed > 0 ? parsed - 1 : count + parsed;
        return index >= 0 && index < count;
    }

    private static void AccumulateGeneratedNormal(
        IReadOnlyList<MeshVertex> vertices,
        IList<Vector3> generatedNormalSums,
        MeshTriangle triangle)
    {
        var faceNormal = Geometry3D.TriangleNormal(
            triangle,
            static item => item.A,
            static item => item.B,
            static item => item.C,
            index => vertices[index].Position,
            Vector3.UnitY);
        AddIfGenerated(triangle.A, faceNormal);
        AddIfGenerated(triangle.B, faceNormal);
        AddIfGenerated(triangle.C, faceNormal);

        void AddIfGenerated(int vertexIndex, Vector3 normal)
        {
            if (vertices[vertexIndex].Normal.LengthSquared() <= 0.0001f)
            {
                generatedNormalSums[vertexIndex] += normal;
            }
        }
    }

    private static void NormalizeGeneratedNormals(List<MeshVertex> vertices, IReadOnlyList<Vector3> generatedNormalSums)
    {
        for (var index = 0; index < vertices.Count; index++)
        {
            if (vertices[index].Normal.LengthSquared() > 0.0001f)
            {
                continue;
            }

            vertices[index] = vertices[index] with
            {
                Normal = Geometry3D.NormalizeOrDefault(generatedNormalSums[index], Vector3.UnitY)
            };
        }
    }

    private static float ParseFloat(string value)
    {
        return float.Parse(value, CultureInfo.InvariantCulture);
    }

    private readonly record struct ObjVertexRef(int PositionIndex, int NormalIndex);

    private readonly record struct ObjVertexKey(int PositionIndex, int NormalIndex);
}
