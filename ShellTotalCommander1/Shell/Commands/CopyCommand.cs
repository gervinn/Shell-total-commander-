using System.IO;

namespace ShellTotalCommander1.Shell.Commands;

public sealed class CopyCommand : ShellCommandBase
{
    public override string Name => "copy";

    public override string Description => "Copies files or directories. Usage: copy <source> <destination>.";

    protected override CommandResult ExecuteCore(ShellContext context, string[] args)
    {
        if (args.Length < 2)
        {
            return CommandResult.Failure("Usage: copy <source> <destination>");
        }

        var source = ResolvePath(context, args[0]);
        var destination = ResolvePath(context, args[1]);

        if (Directory.Exists(source))
        {
            CopyDirectory(source, destination);
        }
        else if (File.Exists(source))
        {
            var destinationDirectory = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            File.Copy(source, destination, overwrite: true);
        }
        else
        {
            return CommandResult.Failure($"Source '{source}' not found.");
        }

        return CommandResult.SuccessResult($"Copied '{source}' to '{destination}'.");
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        var sourceDirectory = new DirectoryInfo(sourceDir);
        if (!sourceDirectory.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory '{sourceDir}' not found.");
        }

        Directory.CreateDirectory(destinationDir);

        foreach (var file in sourceDirectory.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, overwrite: true);
        }

        foreach (var directory in sourceDirectory.GetDirectories())
        {
            var targetSubDir = Path.Combine(destinationDir, directory.Name);
            CopyDirectory(directory.FullName, targetSubDir);
        }
    }
}
