using System.Threading.Tasks;

namespace PseudoCLI.Command
{
    internal sealed class ClsCommand : ICommand
    {
        public string Name => "cls";
        public string Help => "Clear the screen.";

        public Task<int> ExecuteAsync(string args, ShellState state)
        {
            System.Console.Clear();
            return Task.FromResult(0);
        }
    }
}
