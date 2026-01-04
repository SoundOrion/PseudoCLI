using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PseudoCLI.Command
{
    internal sealed class SetCommand : ICommand
    {
        public string Name => "set";
        public string Help => "Set or show environment variables. (set [name[=value]])";

        public async Task<int> ExecuteAsync(string args, ShellState state)
        {
            args = (args ?? "").Trim();

            if (args.Length == 0)
            {
                var all = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (DictionaryEntry kv in Environment.GetEnvironmentVariables())
                    all[(string)kv.Key] = (string)kv.Value;

                foreach (var kv in state.Env)
                    all[kv.Key] = kv.Value;

                foreach (var kv in all)
                    Console.WriteLine($"{kv.Key}={kv.Value}");

                return 0;
            }

            int eq = args.IndexOf('=');
            if (eq < 0)
            {
                // cmd.exe に委譲（set PATH 等）
                return await CmdRunner.RunCmdStreamingAsync("set " + args, state);
            }

            string name = args.Substring(0, eq).Trim();
            string value = (eq + 1 < args.Length) ? args.Substring(eq + 1) : "";

            if (name.Length == 0)
            {
                Console.Error.WriteLine("Invalid syntax.");
                return 1;
            }

            state.Env[name] = value;
            return 0;
        }
    }
}
