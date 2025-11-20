using System.IO;

namespace ShellTotalCommander1.Shell.Commands;

public sealed class ChangeDirectoryCommand : ShellCommandBase
{
    public override string Name => "cd";

    public override string Description => "Changes the current directory (supports drive switching).";

    protected override CommandResult ExecuteCore(ShellContext context, string[] args)
    {
        if (args.Length == 0)
        {
            return CommandResult.SuccessResult(context.CurrentDirectory.FullName);
        }

        var target = ResolvePath(context, args[0]);
        if (!Directory.Exists(target))
        {
            return CommandResult.Failure($"Directory '{target}' not found.");
        }

        context.ChangeDirectory(target);
        return CommandResult.SuccessResult($"Current directory: {context.CurrentDirectory.FullName}");
    }
}
