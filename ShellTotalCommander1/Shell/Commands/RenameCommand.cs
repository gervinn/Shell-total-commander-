using System.IO;

namespace ShellTotalCommander1.Shell.Commands;

/// <summary>
/// Renames a file or directory. Usage: rename &lt;source&gt; &lt;newName&gt;.
/// This command demonstrates an additional file operation beyond the original
/// requirements and can be used to rename both files and folders. If the
/// destination exists it will be overwritten. Relative paths are resolved
/// against the current directory.
/// </summary>
public sealed class RenameCommand : ShellCommandBase
{
    public override string Name => "rename";

    public override string Description => "Renames a file or directory. Usage: rename <source> <newName>.";

    protected override CommandResult ExecuteCore(ShellContext context, string[] args)
    {
        if (args.Length < 2)
        {
            return CommandResult.Failure("Usage: rename <source> <newName>");
        }

        var sourcePath = ResolvePath(context, args[0]);
        var newName = args[1];
        if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
        {
            return CommandResult.Failure($"Path '{sourcePath}' not found.");
        }

        var directory = Path.GetDirectoryName(sourcePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = context.CurrentDirectory.FullName;
        }
        var destinationPath = Path.Combine(directory!, newName);

        try
        {
            if (File.Exists(sourcePath))
            {
                // Overwrite if destination exists
                if (File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                }
                File.Move(sourcePath, destinationPath);
            }
            else if (Directory.Exists(sourcePath))
            {
                if (Directory.Exists(destinationPath))
                {
                    Directory.Delete(destinationPath, recursive: true);
                }
                Directory.Move(sourcePath, destinationPath);
            }
        }
        catch (IOException ex)
        {
            return CommandResult.Failure(ex.Message);
        }

        return CommandResult.SuccessResult($"Renamed '{sourcePath}' to '{destinationPath}'.");
    }
}