using STFU.Mesh;

namespace STFU.NPR.Analysis;

public sealed class GeometryAnalyzer
{
    public MeshAnalysisCache Analyze(MeshData mesh)
    {
        var store = new MeshAnalysisCacheStore();
        return store.GetOrCreate(default, mesh);
    }
}
