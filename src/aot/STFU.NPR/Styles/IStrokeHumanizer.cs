using STFU.NPR.Graph;

namespace STFU.NPR.Styles;

public interface IStrokeHumanizer
{
    void Humanize(StyledStroke stroke, NprStrokeStyle style, int globalSeed);
}
