using System.IO;
using System.Linq;

namespace ShellTotalCommander1.Shell.Commands;

public sealed class ListCommand : ShellCommandBase
{
    public override string Name => "ls";

    public override string Description => "Lists files and directories in the current or specified path.";

    protected override CommandResult ExecuteCore(ShellContext context, string[] args)
    {
        var targetPath = args.Length > 0 ? ResolvePath(context, args[0]) : context.CurrentDirectory.FullName;
        if (!Directory.Exists(targetPath))
        {
            return CommandResult.Failure($"Directory '{targetPath}' not found.");
        }

        var directory = new DirectoryInfo(targetPath);
        var items = directory
            .EnumerateFileSystemInfos()
            .OrderByDescending(info => info is DirectoryInfo)
            .ThenBy(info => info.Name)
            .ToList();

        return CommandResult.SuccessResult($"Listing for {directory.FullName}", ToItems(items));
    }
}
