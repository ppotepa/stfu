using STFU.Messaging.Commands;

namespace STFU.Abstractions.Modules;

public interface IModuleContext
{
    IModuleServiceRegistry Services { get; }

    ICommandRegistry Commands { get; }
}
