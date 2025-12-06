using System;
using System.Collections.Generic;
using System.IO;
using ShellTotalCommander1.Shell.Commands;
using ShellTotalCommander1.Shell.Factory;
using ShellTotalCommander1.Shell.Interpreter;
using ShellTotalCommander1.Shell.Prototype;
using ShellTotalCommander1.Shell.States;

namespace ShellTotalCommander1.Shell;

public sealed class ShellContext
{
    private IShellState _state;

    public ShellContext(
        string? startDirectory = null,
        ICommandFactory? commandFactory = null,
        ICommandInterpreter? interpreter = null,
        IShellState? initialState = null)
    {
        var directoryPath = string.IsNullOrWhiteSpace(startDirectory) ? Environment.CurrentDirectory : startDirectory;
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory '{directoryPath}' does not exist.");
        }

        CurrentDirectory = new DirectoryInfo(directoryPath);
        CommandFactory = commandFactory ?? CreateDefaultFactory();
        Interpreter = interpreter ?? new SimpleCommandInterpreter();
        _state = initialState ?? new BrowsingState();
    }

    public DirectoryInfo CurrentDirectory { get; private set; }

    public ICommandFactory CommandFactory { get; }

    public ICommandInterpreter Interpreter { get; }

    public IShellState CurrentState => _state;

    public CommandResult Execute(string input)
    {
        try
        {
            var parsed = Interpreter.Parse(input);
            if (parsed.IsEmpty)
            {
                return CommandResult.Failure("Command cannot be empty.");
            }

            return _state.HandleCommand(this, parsed);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure(ex.Message);
        }
    }

    public void ChangeState(IShellState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public void ChangeDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Directory '{path}' not found.");
        }

        CurrentDirectory = new DirectoryInfo(path);
    }

    public IEnumerable<IShellCommand> GetAvailableCommands() => CommandFactory.GetAvailableCommands();

    private static ICommandFactory CreateDefaultFactory()
    {
        var registry = new CommandPrototypeRegistry();
        registry.Register(new HelpCommand());
        registry.Register(new ListCommand());
        registry.Register(new ChangeDirectoryCommand());
        registry.Register(new PwdCommand());
        registry.Register(new CopyCommand());
        registry.Register(new MoveCommand());
        registry.Register(new DeleteCommand());
        registry.Register(new SearchCommand());
        registry.Register(new DrivesCommand());
        // Additional command for renaming files and folders.
        registry.Register(new RenameCommand());
        return new CommandFactory(registry);
    }
}
