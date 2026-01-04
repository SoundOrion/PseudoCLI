using System.Threading.Tasks;

namespace PseudoCLI.Command
{
    internal sealed class EchoCommand : ICommand
    {
        public string Name => "echo";
        public string Help => "Echo text. (echo <text>)";

        public Task<int> ExecuteAsync(string args, ShellState state)
        {
            // cmd.exe の echo に寄せるなら "echo." なども実装できるけど、まずは素直に
            System.Console.WriteLine(args ?? "");
            return Task.FromResult(0);
        }
    }
}
