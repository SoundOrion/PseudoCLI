using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PseudoCLI
{
    internal sealed class Builtins
    {
        private readonly ShellState _state;
        private readonly CmdRunner _runner;

        // コマンド名 → ハンドラ
        private readonly Dictionary<string, Func<string, Task<bool>>> _handlers
            = new Dictionary<string, Func<string, Task<bool>>>(StringComparer.OrdinalIgnoreCase);

        public Builtins(ShellState state, CmdRunner runner)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _runner = runner;

            // ここで登録（追加が楽）
            Register("cd", HandleCdAsync);
            Register("set", HandleSetAsync);

            // 例：エイリアスも簡単
            // Register("chdir", HandleCdAsync);
        }

        public void Register(string name, Func<string, Task<bool>> handler)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name is empty.", nameof(name));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _handlers[name] = handler;
        }

        /// <summary>
        /// 入力行が builtin なら実行して true、違うなら false
        /// </summary>
        public Task<bool> TryHandleAsync(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Task.FromResult(false);

            var (cmd, rest) = SplitCommand(input);

            if (cmd.Length == 0)
                return Task.FromResult(false);

            if (_handlers.TryGetValue(cmd, out var handler))
                return handler(rest);

            return Task.FromResult(false);
        }

        // ----------------------------
        // cd
        // ----------------------------
        private Task<bool> HandleCdAsync(string rest)
        {
            var arg = (rest ?? "").Trim();

            if (arg.Length == 0)
            {
                Console.WriteLine(_state.Cwd);
                return Task.FromResult(true);
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
                    return Task.FromResult(true);
                }

                _state.Cwd = target;
                Environment.CurrentDirectory = target;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }

            return Task.FromResult(true);
        }

        // ----------------------------
        // set
        // ----------------------------
        private async Task<bool> HandleSetAsync(string rest)
        {
            var arg = (rest ?? "").Trim();

            // set （一覧表示）
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
                await _runner.RunCmdStreamingAsync("set " + arg, _state);
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

        // ----------------------------
        // 入力からコマンドと残りを分離
        // 例: "cd  aaa" -> ("cd", "aaa")
        //     "set"     -> ("set", "")
        // ----------------------------
        private static (string cmd, string rest) SplitCommand(string input)
        {
            input = input.TrimStart();

            int i = 0;
            while (i < input.Length && !char.IsWhiteSpace(input[i]))
                i++;

            var cmd = input.Substring(0, i);
            var rest = (i < input.Length) ? input.Substring(i).TrimStart() : "";

            return (cmd, rest);
        }
    }
}
