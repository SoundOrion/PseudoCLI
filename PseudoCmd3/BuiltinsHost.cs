using PseudoCLI.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PseudoCLI
{
    internal sealed class BuiltinsHost
    {
        private readonly Dictionary<string, ICommand> _commands =
            new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase);

        public BuiltinsHost(IEnumerable<ICommand> commands)
        {
            foreach (var c in commands)
                Register(c);
        }

        public void Register(ICommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (string.IsNullOrWhiteSpace(command.Name)) throw new ArgumentException("Command.Name is empty.");
            _commands[command.Name] = command;
        }

        public bool Has(string name) => _commands.ContainsKey(name);

        public async Task<bool> TryHandleAsync(string input, ShellState state)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            var (name, args) = SplitCommand(input);

            // help は自動提供（予約語）
            if (name.Equals("help", StringComparison.OrdinalIgnoreCase) || name.Equals("?", StringComparison.OrdinalIgnoreCase))
            {
                PrintHelp(args);
                return true;
            }

            if (_commands.TryGetValue(name, out var cmd))
            {
                int code = await cmd.ExecuteAsync(args, state);
                // code を使って何かしたければここで（今は表示しない）
                return true;
            }

            return false;
        }

        public void PrintHelp(string args)
        {
            args = (args ?? "").Trim();

            if (args.Length == 0)
            {
                Console.WriteLine("Built-in commands:");
                foreach (var cmd in _commands.Values.OrderBy(c => c.Name))
                    Console.WriteLine($"  {cmd.Name,-8} {cmd.Help}");

                Console.WriteLine();
                Console.WriteLine("Type: help <command>");
                return;
            }

            if (_commands.TryGetValue(args, out var c2))
            {
                Console.WriteLine($"{c2.Name}: {c2.Help}");
                return;
            }

            Console.WriteLine($"Unknown command: {args}");
        }

        private static (string name, string args) SplitCommand(string input)
        {
            input = input.TrimStart();
            int i = 0;
            while (i < input.Length && !char.IsWhiteSpace(input[i])) i++;
            var name = input.Substring(0, i);
            var args = (i < input.Length) ? input.Substring(i).TrimStart() : "";
            return (name, args);
        }
    }
}
