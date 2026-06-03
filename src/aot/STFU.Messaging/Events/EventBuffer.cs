namespace STFU.Messaging.Events;

public sealed class EventBuffer
{
    private readonly Queue<IEvent> _events = [];

    public int Count => _events.Count;

    public void Enqueue(IEvent @event)
    {
        _events.Enqueue(@event);
    }

    public bool TryDequeue(out IEvent @event)
    {
        return _events.TryDequeue(out @event!);
    }
}
