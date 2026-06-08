using STFU.Common.Primitives;
using STFU.Messaging.Commands;

namespace STFU.Engine.Commands;

public readonly record struct RenameEntityCommand(
    EntityId EntityId,
    string Name) : ICommand;
