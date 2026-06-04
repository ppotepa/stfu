namespace STFU.NPR.Graph;

[Flags]
public enum FeatureCurveFlags
{
    None = 0,
    ViewDependent = 1 << 0,
    Generated = 1 << 1
}
