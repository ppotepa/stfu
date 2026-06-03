using STFU.Common.Primitives;
using STFU.Messaging.Commands;

namespace STFU.Engine.Commands;

public readonly record struct DeleteEntityCommand(EntityId EntityId) : ICommand;
