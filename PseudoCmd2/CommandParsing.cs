using System;

namespace PseudoCLI
{
    internal static class CommandParsing
    {
        public static bool StartsWithCommand(string input, string cmd)
        {
            if (!input.StartsWith(cmd, StringComparison.OrdinalIgnoreCase)) return false;
            if (input.Length == cmd.Length) return true;
            return char.IsWhiteSpace(input[cmd.Length]);
        }
    }
}
