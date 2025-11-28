using ShellTotalCommander1.Shell.Commands;
using ShellTotalCommander1.Shell.Interpreter;

namespace ShellTotalCommander1.Shell.States;

public sealed class BrowsingState : ShellState
{
    public CommandResult HandleCommand(ShellContext context, ParsedCommand command)
    {
        if (command.IsEmpty)
        {
            return CommandResult.Failure("Command is empty.");
        }

        var shellCommand = context.CommandFactory.Create(command.Name);
        if (shellCommand is null)
        {
            return CommandResult.Failure($"Unknown command '{command.Name}'. Type 'help' to list available commands.");
        }

        return shellCommand.Execute(context, command.Arguments);
    }
}
