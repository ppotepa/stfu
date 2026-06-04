namespace STFU.Messaging.Commands;

public interface ICommandRegistry
{
    ICommandRegistry Register<TCommand>(ICommandHandler<TCommand> handler)
        where TCommand : ICommand;
}
