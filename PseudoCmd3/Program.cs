using PseudoCLI.Command;
using System;
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

            Console.WriteLine($"Microsoft Windows [Version {Environment.OSVersion.Version}]");
            Console.WriteLine("(c) Microsoft Corporation. All rights reserved.");
            Console.WriteLine();

            var state = new ShellState().CreateDefault();

            var builtins = new BuiltinsHost(new ICommand[]
            {
                new CdCommand(),
                new SetCommand(),
                new ClsCommand(),
                new PwdCommand(),
                new EchoCommand(),
                new PromptCommand(),
            });

            while (true)
            {
                Console.Write(state.RenderPrompt());

                var line = Console.ReadLine();
                if (line == null) break;

                line = line.Trim();
                if (line.Length == 0) continue;

                if (line.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    return 0;

                if (await builtins.TryHandleAsync(line, state))
                    continue;

                await CmdRunner.RunCmdStreamingAsync(line, state);
            }

            return 0;
        }
    }
}
