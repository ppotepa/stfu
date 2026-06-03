namespace STFU.Messaging.Commands;

public sealed class CommandBuffer
{
    private readonly Queue<ICommand> _commands = [];

    public int Count => _commands.Count;

    public void Enqueue(ICommand command)
    {
        _commands.Enqueue(command);
    }

    public bool TryDequeue(out ICommand command)
    {
        return _commands.TryDequeue(out command!);
    }
}
