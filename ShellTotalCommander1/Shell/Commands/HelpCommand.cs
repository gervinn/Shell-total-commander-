using System.Linq;
using System.Text;

namespace ShellTotalCommander1.Shell.Commands;

public sealed class HelpCommand : ShellCommandBase
{
    public override string Name => "help";

    public override string Description => "Displays available commands.";

    protected override CommandResult ExecuteCore(ShellContext context, string[] args)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Available commands:");

        foreach (var command in context.GetAvailableCommands().OrderBy(c => c.Name))
        {
            builder.AppendLine($" - {command.Name}: {command.Description}");
        }

        return CommandResult.SuccessResult(builder.ToString().TrimEnd());
    }
}
