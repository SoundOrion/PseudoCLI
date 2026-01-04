using System.Threading.Tasks;

namespace PseudoCLI.Command
{
    internal interface ICommand
    {
        string Name { get; }        // コマンド名（例: "cd"）
        string Help { get; }        // 1行説明
        Task<int> ExecuteAsync(string args, ShellState state); // 0=成功、それ以外=エラーコード
    }
}
