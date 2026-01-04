using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PseudoCLI
{
    internal static class Program
    {
        static async Task<int> Main()
        {
            Console.Title = "PseudoCLI";
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            var version =
                Assembly.GetExecutingAssembly()
                        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                        .InformationalVersion
                ?? "unknown";

            Console.WriteLine($"Microsoft Windows [Version {Environment.OSVersion.Version}]");
            Console.WriteLine("(c) Microsoft Corporation. All rights reserved.");
            Console.WriteLine($"PseudoCLI Version {version}");
            Console.WriteLine();

            var state = new ShellState().CreateDefault();
            var builtins = new Builtins(state);

            while (true)
            {
                Console.Write($"{state.Cwd}>");
                var line = Console.ReadLine();
                if (line == null) break;

                line = line.Trim();
                if (line.Length == 0) continue;

                if (line.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    return 0;

                if (await builtins.TryHandleAsync(line))
                    continue;

                await CmdRunner.RunCmdStreamingAsync(line, state);
            }

            return 0;
        }
    }
}
