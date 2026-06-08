namespace STFU.UI;

internal sealed class ViewportFrameState
{
    public Dictionary<long, ViewportRuntimeStatus> RuntimeStatusByRevision { get; } = new();

    public long NextRevision;
    public long LastLoggedRevision;
    public long LastEnqueuedRevision;
    public long LastCompletedRevision;

    public bool RenderInFlight;
    public bool LastPresentedWithGpuTexture;
    public bool DeferredFrameRequested;

    public int ConsecutiveDirectPresentSkips;
    public bool DirectPresentFallbackNotified;
    public int DeferredFrameLogCounter;

    private int _lastViewportWidth;
    private int _lastViewportHeight;

    public bool IsDirectGpuPresenting(DirectXViewportPresenter? presenter, int fallbackThreshold)
    {
        return LastPresentedWithGpuTexture &&
            presenter?.IsAttached == true &&
            ConsecutiveDirectPresentSkips < fallbackThreshold;
    }

    public bool UpdateViewportSize(int width, int height)
    {
        var changed = _lastViewportWidth > 0 &&
            _lastViewportHeight > 0 &&
            (width != _lastViewportWidth || height != _lastViewportHeight);

        _lastViewportWidth = width;
        _lastViewportHeight = height;
        return changed;
    }

    public void ResetDirectPresentFailures()
    {
        ConsecutiveDirectPresentSkips = 0;
        DirectPresentFallbackNotified = false;
    }

    public bool RecordDeferredFrame()
    {
        DeferredFrameRequested = true;
        DeferredFrameLogCounter++;
        return DeferredFrameLogCounter % 120 == 0;
    }

    public void RememberRuntimeStatus(long revision, ViewportRuntimeStatus status)
    {
        RuntimeStatusByRevision[revision] = status;
    }

    public ViewportRuntimeStatus? ConsumeRuntimeStatus(long revision)
    {
        if (!RuntimeStatusByRevision.Remove(revision, out var status))
        {
            return null;
        }

        return status;
    }

    public void CleanupRuntimeStatuses(long completedRevision)
    {
        if (RuntimeStatusByRevision.Count == 0)
        {
            return;
        }

        var staleRevisions = RuntimeStatusByRevision.Keys
            .Where(revision => revision <= completedRevision)
            .ToArray();

        foreach (var staleRevision in staleRevisions)
        {
            RuntimeStatusByRevision.Remove(staleRevision);
        }
    }
}
