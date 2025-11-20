using System.IO;
using System.Linq;

namespace ShellTotalCommander1.Shell.Commands;

public sealed class DrivesCommand : ShellCommandBase
{
    public override string Name => "drives";

    public override string Description => "Lists available logical drives.";

    protected override CommandResult ExecuteCore(ShellContext context, string[] args)
    {
        var drives = DriveInfo.GetDrives()
            .Where(drive => drive.IsReady)
            .Select(drive => new DirectoryInfo(drive.RootDirectory.FullName))
            .Cast<FileSystemInfo>()
            .ToList();

        if (drives.Count == 0)
        {
            return CommandResult.SuccessResult("No drives available.");
        }

        return CommandResult.SuccessResult("Available drives:", ToItems(drives));
    }
}
