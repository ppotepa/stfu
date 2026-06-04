using STFU.NPR.Pipeline.Default.Steps;

namespace STFU.NPR.Pipeline.Default;

public static class DefaultPipeline
{
    public static STFU.NPR.Pipeline.INprPipeline Create()
    {
        return new STFU.NPR.Pipeline.NprPipeline<
            ProjectMeshStep,
            BuildProjectedTrianglesStep,
            BuildMeshTopologyStep,
            DefaultBuildFaceIdVisibilityBufferStep,
            DefaultClassifyEdgesToFragmentsStep,
            DefaultBuildPathsFromFragmentsStep,
            DefaultSimplifyAndSortPathsStep,
            DefaultApplyDrawProgressStep,
            DefaultBuildInkFrameStep,
            DefaultBuildDebugFrameStep>();
    }
}
