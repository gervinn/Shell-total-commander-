using ShellTotalCommander1.Shell.Commands;

namespace ShellTotalCommander1.Shell.Prototype;

public interface ICommandPrototype
{
    string Name { get; }

    IShellCommand Clone();
}
