using System.Threading.Tasks;

namespace PseudoCLI.Command
{
    internal sealed class PwdCommand : ICommand
    {
        public string Name => "pwd";
        public string Help => "Print working directory.";

        public Task<int> ExecuteAsync(string args, ShellState state, CmdRunner runner)
        {
            System.Console.WriteLine(state.Cwd);
            return Task.FromResult(0);
        }
    }
}
