using ShellTotalCommander1.Shell.Commands;
using ShellTotalCommander1.Shell.Interpreter;

namespace ShellTotalCommander1.Shell.States;

public interface IShellState
{
    CommandResult HandleCommand(ShellContext context, ParsedCommand command);
}
