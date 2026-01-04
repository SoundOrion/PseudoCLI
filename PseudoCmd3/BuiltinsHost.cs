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

        public async Task<bool> TryHandleAsync(string input, ShellState state, CmdRunner runner)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            var (name, args) = SplitCommand(input);

            // help は自動提供（予約語）
            if (name.Equals("help", StringComparison.OrdinalIgnoreCase) || name.Equals("?", StringComparison.OrdinalIgnoreCase))
            {
                PrintHelp(args);
                return true;
            }

            // 1) まず普通に辞書で探す
            if (_commands.TryGetValue(name, out var cmd))
            {
                await cmd.ExecuteAsync(args, state, runner);
                return true;
            }

            // 2) 見つからなければ「cmd/args」形式を救済（例: cd/d → cd + /d）
            if (TrySplitWithSlash(input, out var name2, out var args2) &&
                _commands.TryGetValue(name2, out var cmd2))
            {
                await cmd2.ExecuteAsync(args2, state, runner);
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

        // 例: "cd/d foo" -> name="cd", args="/d foo"
        //     "cd/d"     -> name="cd", args="/d"
        private bool TrySplitWithSlash(string input, out string name, out string args)
        {
            name = "";
            args = "";

            // 登録済みコマンド名のうち、"name/" で始まる最長一致を探す
            // (例: 将来 "foo" と "foo2" があっても安全)
            string best = null;

            foreach (var key in _commands.Keys)
            {
                if (input.Length <= key.Length) continue;
                if (!input.StartsWith(key, StringComparison.OrdinalIgnoreCase)) continue;
                if (input[key.Length] != '/') continue;

                if (best == null || key.Length > best.Length)
                    best = key;
            }

            if (best == null) return false;

            name = best;
            args = input.Substring(best.Length).TrimStart(); // "/" から後ろ（例: "/d ...")
            return true;
        }
    }
}
