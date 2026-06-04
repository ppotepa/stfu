using STFU.Messaging.Commands;
using STFU.NPR.Commands;
using STFU.NPR.Composition;

namespace STFU.NPR.Handlers;

public sealed class SetEntityNprRoleCommandHandler : ICommandHandler<SetEntityNprRoleCommand>
{
    private readonly NprEntityStyleRegistry _styles;

    public SetEntityNprRoleCommandHandler(NprEntityStyleRegistry styles)
    {
        _styles = styles;
    }

    public void Handle(SetEntityNprRoleCommand command)
    {
        _styles.SetRole(command.EntityId, command.Role);
    }
}
