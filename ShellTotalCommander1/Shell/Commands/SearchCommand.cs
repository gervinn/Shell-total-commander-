using System;
using System.Collections.Generic;
using System.IO;

namespace ShellTotalCommander1.Shell.Commands;

public sealed class SearchCommand : ShellCommandBase
{
    public override string Name => "search";

    public override string Description => "Searches for files or directories. Usage: search <pattern> [path].";

    protected override CommandResult ExecuteCore(ShellContext context, string[] args)
    {
        if (args.Length == 0)
        {
            return CommandResult.Failure("Usage: search <pattern> [path]");
        }

        var pattern = args[0];
        var startPath = args.Length > 1 ? ResolvePath(context, args[1]) : context.CurrentDirectory.FullName;
        if (!Directory.Exists(startPath))
        {
            return CommandResult.Failure($"Directory '{startPath}' not found.");
        }

        var results = new List<FileSystemInfo>();
        var directoryQueue = new Queue<DirectoryInfo>();
        directoryQueue.Enqueue(new DirectoryInfo(startPath));

        while (directoryQueue.Count > 0)
        {
            var current = directoryQueue.Dequeue();
            try
            {
                foreach (var match in current.EnumerateFileSystemInfos(pattern))
                {
                    results.Add(match);
                }

                foreach (var subDirectory in current.EnumerateDirectories())
                {
                    directoryQueue.Enqueue(subDirectory);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories without access rights.
            }
        }

        return CommandResult.SuccessResult($"Found {results.Count} item(s).", ToItems(results));
    }
}
