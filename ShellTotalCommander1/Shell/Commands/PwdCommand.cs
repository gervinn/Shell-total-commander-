namespace ShellTotalCommander1.Shell.Commands;

public sealed class PwdCommand : ShellCommandBase
{
    public override string Name => "pwd";

    public override string Description => "Displays the current directory path.";

    protected override CommandResult ExecuteCore(ShellContext context, string[] args)
    {
        return CommandResult.SuccessResult(context.CurrentDirectory.FullName);
    }
}
