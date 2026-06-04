namespace STFU.Messaging.Commands;

public sealed class CommandDispatcher : ICommandRegistry
{
    private readonly Dictionary<Type, ICommandHandlerAdapter> _handlers = new();

    public int HandlerCount => _handlers.Count;

    public ICommandRegistry Register<TCommand>(ICommandHandler<TCommand> handler)
        where TCommand : ICommand
    {
        _handlers[typeof(TCommand)] = new CommandHandlerAdapter<TCommand>(handler);
        return this;
    }

    public bool Dispatch(ICommand command)
    {
        if (!_handlers.TryGetValue(command.GetType(), out var handler))
        {
            return false;
        }

        handler.Handle(command);
        return true;
    }

    public int DispatchAll(CommandBuffer buffer)
    {
        var handled = 0;

        while (buffer.TryDequeue(out var command))
        {
            if (Dispatch(command))
            {
                handled++;
            }
        }

        return handled;
    }

    private interface ICommandHandlerAdapter
    {
        void Handle(ICommand command);
    }

    private sealed class CommandHandlerAdapter<TCommand> : ICommandHandlerAdapter
        where TCommand : ICommand
    {
        private readonly ICommandHandler<TCommand> _handler;

        public CommandHandlerAdapter(ICommandHandler<TCommand> handler)
        {
            _handler = handler;
        }

        public void Handle(ICommand command)
        {
            _handler.Handle((TCommand)command);
        }
    }
}
