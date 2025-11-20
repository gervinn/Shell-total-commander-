using System;
using System.Collections.Generic;

namespace ShellTotalCommander1.Shell.Prototype;

public sealed class CommandPrototypeRegistry
{
    private readonly Dictionary<string, ICommandPrototype> _prototypes = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ICommandPrototype prototype)
    {
        if (prototype is null)
        {
            throw new ArgumentNullException(nameof(prototype));
        }

        _prototypes[prototype.Name] = prototype;
    }

    public bool TryGetPrototype(string name, out ICommandPrototype prototype)
    {
        return _prototypes.TryGetValue(name, out prototype!);
    }

    public IEnumerable<ICommandPrototype> GetAll() => _prototypes.Values;
}
