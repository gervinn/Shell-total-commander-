using System;
using System.Collections.Generic;

namespace ShellTotalCommander1.Shell.Commands;

/// <summary>
/// Represents the outcome of executing a command.
/// </summary>
public sealed class CommandResult
{
    private CommandResult(bool success, string message, IReadOnlyList<FileItem>? items)
    {
        Success = success;
        Message = message;
        Items = items ?? Array.Empty<FileItem>();
    }

    public bool Success { get; }

    public string Message { get; }

    public IReadOnlyList<FileItem> Items { get; }

    public static CommandResult SuccessResult(string message, IReadOnlyList<FileItem>? items = null)
        => new(true, message, items);

    public static CommandResult Failure(string message)
        => new(false, message, null);
}
