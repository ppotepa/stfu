using STFU.Messaging.Commands;

namespace STFU.Engine.Commands;

public readonly record struct CreateEntityCommand(string Name) : ICommand;
