using System.Collections.Generic;
using ShellTotalCommander1.Shell.Commands;

namespace ShellTotalCommander1.Shell.Factory;

public interface ICommandFactory
{
    IShellCommand? Create(string name);

    IEnumerable<IShellCommand> GetAvailableCommands();
}
