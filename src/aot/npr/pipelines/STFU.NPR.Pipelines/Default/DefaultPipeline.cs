using STFU.NPR.Pipeline.ReferenceQuality;

namespace STFU.NPR.Pipeline.Default;

public static class DefaultPipeline
{
    public static STFU.NPR.Pipeline.INprPipeline Create()
    {
        return ReferenceQualityPipeline.Create();
    }
}
