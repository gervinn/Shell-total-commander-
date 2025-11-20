using System.IO;

namespace ShellTotalCommander1.Shell.Commands;

public sealed class MoveCommand : ShellCommandBase
{
    public override string Name => "move";

    public override string Description => "Moves files or directories. Usage: move <source> <destination>.";

    protected override CommandResult ExecuteCore(ShellContext context, string[] args)
    {
        if (args.Length < 2)
        {
            return CommandResult.Failure("Usage: move <source> <destination>");
        }

        var source = ResolvePath(context, args[0]);
        var destination = ResolvePath(context, args[1]);

        if (Directory.Exists(source))
        {
            if (Directory.Exists(destination))
            {
                Directory.Delete(destination, recursive: true);
            }

            Directory.Move(source, destination);
        }
        else if (File.Exists(source))
        {
            var destinationDirectory = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            if (File.Exists(destination))
            {
                File.Delete(destination);
            }

            File.Move(source, destination);
        }
        else
        {
            return CommandResult.Failure($"Source '{source}' not found.");
        }

        return CommandResult.SuccessResult($"Moved '{source}' to '{destination}'.");
    }
}
