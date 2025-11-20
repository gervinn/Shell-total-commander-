using System;
using System.Collections.Generic;
using System.Linq;
using ShellTotalCommander1.Shell.Commands;
using ShellTotalCommander1.Shell.Prototype;

namespace ShellTotalCommander1.Shell.Factory;

public sealed class CommandFactory : ICommandFactory
{
    private readonly CommandPrototypeRegistry _registry;

    public CommandFactory(CommandPrototypeRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public IShellCommand? Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        if (_registry.TryGetPrototype(name, out var prototype))
        {
            return prototype.Clone();
        }

        return null;
    }

    public IEnumerable<IShellCommand> GetAvailableCommands()
    {
        return _registry.GetAll().Select(p => p.Clone());
    }
}
