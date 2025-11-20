namespace ShellTotalCommander1.Shell.Interpreter;

public interface ICommandInterpreter
{
    ParsedCommand Parse(string? input);
}
