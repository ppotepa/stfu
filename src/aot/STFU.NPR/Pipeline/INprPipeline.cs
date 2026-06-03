using STFU.Strokes;

namespace STFU.NPR.Pipeline;

public interface INprPipeline
{
    StrokeFrame Execute(NprContext context);
}
