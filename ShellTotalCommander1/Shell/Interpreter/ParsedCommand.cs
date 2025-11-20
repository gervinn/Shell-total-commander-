using System;

namespace ShellTotalCommander1.Shell.Interpreter;

public readonly record struct ParsedCommand(string Name, string[] Arguments)
{
    public static ParsedCommand Empty { get; } = new(string.Empty, Array.Empty<string>());

    public bool IsEmpty => string.IsNullOrWhiteSpace(Name);
}
