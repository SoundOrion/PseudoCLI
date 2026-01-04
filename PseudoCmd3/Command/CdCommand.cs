using System;
using System.IO;
using System.Threading.Tasks;

namespace PseudoCLI.Command
{
    internal sealed class CdCommand : ICommand
    {
        public string Name => "cd";
        public string Help => "Change the current directory. (cd [path])";

        public Task<int> ExecuteAsync(string args, ShellState state)
        {
            args = (args ?? "").Trim();

            if (args.Length == 0)
            {
                Console.WriteLine(state.Cwd);
                return Task.FromResult(0);
            }

            if (args.StartsWith("/d", StringComparison.OrdinalIgnoreCase))
                args = args.Substring(2).Trim();

            try
            {
                var target = Path.GetFullPath(
                    Path.IsPathRooted(args) ? args : Path.Combine(state.Cwd, args));

                if (!Directory.Exists(target))
                {
                    Console.Error.WriteLine("The system cannot find the path specified.");
                    return Task.FromResult(1);
                }

                state.Cwd = target;
                Environment.CurrentDirectory = target;
                return Task.FromResult(0);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return Task.FromResult(1);
            }
        }
    }
}
