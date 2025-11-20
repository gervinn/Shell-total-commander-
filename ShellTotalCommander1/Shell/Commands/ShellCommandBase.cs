using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ShellTotalCommander1.Shell;
using ShellTotalCommander1.Shell.Prototype;

namespace ShellTotalCommander1.Shell.Commands;

/// <summary>
/// Base class for commands implementing template method behavior.
/// </summary>
public abstract class ShellCommandBase : IShellCommand, ICommandPrototype
{
    public abstract string Name { get; }

    public abstract string Description { get; }

    public CommandResult Execute(ShellContext context, string[] args)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        try
        {
            return ExecuteCore(context, args);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure(ex.Message);
        }
    }

    protected abstract CommandResult ExecuteCore(ShellContext context, string[] args);

    public IShellCommand Clone()
    {
        return (IShellCommand)MemberwiseClone();
    }

    protected static string ResolvePath(ShellContext context, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be empty.", nameof(path));
        }

        if (Path.IsPathFullyQualified(path))
        {
            return Path.GetFullPath(path);
        }

        // Handle drive switching commands like "C:" explicitly.
        if (path.Length == 2 && path[1] == ':' && char.IsLetter(path[0]))
        {
            var driveRoot = Path.GetFullPath(path + Path.DirectorySeparatorChar);
            return driveRoot;
        }

        var combined = Path.Combine(context.CurrentDirectory.FullName, path);
        return Path.GetFullPath(combined);
    }

    protected static IReadOnlyList<FileItem> ToItems(IEnumerable<FileSystemInfo> infos)
    {
        return infos.Select(info => new FileItem(info)).ToList();
    }
}
