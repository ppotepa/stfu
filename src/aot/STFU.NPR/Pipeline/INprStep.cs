namespace STFU.NPR.Pipeline;

public interface INprStep
{
    void Execute(NprContext context);
}
