using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace PseudoCLI
{
    internal sealed class Builtins
    {
        private readonly ShellState _state;

        public Builtins(ShellState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public bool TryHandle(string input)
        {
            return TryHandleCd(input) || TryHandleSet(input);
        }

        private bool TryHandleCd(string input)
        {
            if (!CommandParsing.StartsWithCommand(input, "cd"))
                return false;

            var arg = input.Length > 2 ? input.Substring(2).Trim() : "";

            if (arg.Length == 0)
            {
                Console.WriteLine(_state.Cwd);
                return true;
            }

            if (arg.StartsWith("/d", StringComparison.OrdinalIgnoreCase))
                arg = arg.Substring(2).Trim();

            try
            {
                var target = Path.GetFullPath(
                    Path.IsPathRooted(arg) ? arg : Path.Combine(_state.Cwd, arg));

                if (!Directory.Exists(target))
                {
                    Console.Error.WriteLine("The system cannot find the path specified.");
                    return true;
                }

                _state.Cwd = target;
                Environment.CurrentDirectory = target;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }

            return true;
        }

        private bool TryHandleSet(string input)
        {
            if (!CommandParsing.StartsWithCommand(input, "set"))
                return false;

            var arg = input.Length > 3 ? input.Substring(3).Trim() : "";

            if (arg.Length == 0)
            {
                var all = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (DictionaryEntry kv in Environment.GetEnvironmentVariables())
                    all[(string)kv.Key] = (string)kv.Value;

                foreach (var kv in _state.Env)
                    all[kv.Key] = kv.Value;

                foreach (var kv in all)
                    Console.WriteLine($"{kv.Key}={kv.Value}");

                return true;
            }

            int eq = arg.IndexOf('=');
            if (eq < 0)
            {
                // cmd.exe に委譲（set PATH 等）
                CmdRunner.RunCmdStreamingAsync("set " + arg, _state).GetAwaiter().GetResult();
                return true;
            }

            string name = arg.Substring(0, eq).Trim();
            string value = (eq + 1 < arg.Length) ? arg.Substring(eq + 1) : "";

            if (name.Length == 0)
            {
                Console.Error.WriteLine("Invalid syntax.");
                return true;
            }

            _state.Env[name] = value;
            return true;
        }
    }
}
