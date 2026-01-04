using System.Threading.Tasks;

namespace PseudoCLI.Command
{
    internal sealed class PromptCommand : ICommand
    {
        public string Name => "prompt";
        public string Help => "Change prompt. Use $P for path, $G for '>'. (prompt <format>)";

        public Task<int> ExecuteAsync(string args, ShellState state, CmdRunner runner)
        {
            args = (args ?? "").Trim();

            if (args.Length == 0)
            {
                System.Console.WriteLine(state.PromptFormat);
                return Task.FromResult(0);
            }

            state.PromptFormat = args;
            return Task.FromResult(0);
        }
    }
}
