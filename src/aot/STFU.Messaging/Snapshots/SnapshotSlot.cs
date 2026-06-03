namespace STFU.Messaging.Snapshots;

public sealed class SnapshotSlot<TSnapshot>
    where TSnapshot : class, ISnapshot
{
    public TSnapshot? Current { get; private set; }

    public void Publish(TSnapshot snapshot)
    {
        Current = snapshot;
    }
}
