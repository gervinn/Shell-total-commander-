namespace ShellTotalCommander1.Shell.Commands;

public interface IShellCommand
{
    string Name { get; }

    string Description { get; }

    CommandResult Execute(ShellContext context, string[] args);
}
