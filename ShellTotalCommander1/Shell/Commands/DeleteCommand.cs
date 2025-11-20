using System.IO;

namespace ShellTotalCommander1.Shell.Commands;

public sealed class DeleteCommand : ShellCommandBase
{
    public override string Name => "del";

    public override string Description => "Deletes files or directories. Usage: del <path>.";

    protected override CommandResult ExecuteCore(ShellContext context, string[] args)
    {
        if (args.Length == 0)
        {
            return CommandResult.Failure("Usage: del <path>");
        }

        var target = ResolvePath(context, args[0]);
        if (File.Exists(target))
        {
            File.Delete(target);
        }
        else if (Directory.Exists(target))
        {
            Directory.Delete(target, recursive: true);
        }
        else
        {
            return CommandResult.Failure($"Path '{target}' not found.");
        }

        return CommandResult.SuccessResult($"Deleted '{target}'.");
    }
}
