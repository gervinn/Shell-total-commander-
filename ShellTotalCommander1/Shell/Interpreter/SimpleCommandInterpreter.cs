using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShellTotalCommander1.Shell.Interpreter;

public sealed class SimpleCommandInterpreter : ICommandInterpreter
{
    public ParsedCommand Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return ParsedCommand.Empty;
        }

        var tokens = Tokenize(input);
        if (tokens.Count == 0)
        {
            return ParsedCommand.Empty;
        }

        var name = tokens[0];
        var args = tokens.Count > 1 ? tokens.Skip(1).ToArray() : Array.Empty<string>();
        return new ParsedCommand(name, args);
    }

    private static List<string> Tokenize(string input)
    {
        var builder = new StringBuilder();
        var tokens = new List<string>();
        var inQuotes = false;

        for (var i = 0; i < input.Length; i++)
        {
            var current = input[i];

            if (current == '\"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(current) && !inQuotes)
            {
                CommitToken();
                continue;
            }

            builder.Append(current);
        }

        if (inQuotes)
        {
            throw new FormatException("Unmatched quotes in command.");
        }

        CommitToken();
        return tokens;

        void CommitToken()
        {
            if (builder.Length > 0)
            {
                tokens.Add(builder.ToString());
                builder.Clear();
            }
        }
    }
}
