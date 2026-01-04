using System.Diagnostics;
using System.Text;

class Program
{
    static async Task<int> Main()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        Console.Title = "PseudoCLI";
        Console.OutputEncoding = Encoding.GetEncoding(932);
        Console.InputEncoding = Encoding.GetEncoding(932);

        // cmdっぽいヘッダ
        Console.WriteLine($"Microsoft Windows [Version {Environment.OSVersion.Version}]");
        Console.WriteLine("(c) Microsoft Corporation. All rights reserved.");
        Console.WriteLine();

        var state = new ShellState
        {
            Cwd = Environment.CurrentDirectory
        };

        while (true)
        {
            Console.Write($"{state.Cwd}>");
            var line = Console.ReadLine();
            if (line == null) break;

            line = line.Trim();
            if (line.Length == 0) continue;

            if (line.Equals("exit", StringComparison.OrdinalIgnoreCase))
                return 0;

            if (TryHandleCd(line, state)) continue;
            if (TryHandleSet(line, state)) continue;

            await RunCmdStreamingAsync(line, state);
        }

        return 0;
    }

    // ----------------------------
    // 状態保持
    // ----------------------------
    sealed class ShellState
    {
        public string Cwd = "";
        public Dictionary<string, string> Env = new(StringComparer.OrdinalIgnoreCase);
    }

    // ----------------------------
    // cd
    // ----------------------------
    static bool TryHandleCd(string input, ShellState state)
    {
        if (!StartsWithCommand(input, "cd"))
            return false;

        var arg = input.Length > 2 ? input[2..].Trim() : "";

        if (arg.Length == 0)
        {
            Console.WriteLine(state.Cwd);
            return true;
        }

        if (arg.StartsWith("/d", StringComparison.OrdinalIgnoreCase))
            arg = arg[2..].Trim();

        try
        {
            var target = Path.GetFullPath(
                Path.IsPathRooted(arg) ? arg : Path.Combine(state.Cwd, arg));

            if (!Directory.Exists(target))
            {
                Console.Error.WriteLine("The system cannot find the path specified.");
                return true;
            }

            state.Cwd = target;
            Environment.CurrentDirectory = target;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
        }

        return true;
    }

    // ----------------------------
    // set
    // ----------------------------
    static bool TryHandleSet(string input, ShellState state)
    {
        if (!StartsWithCommand(input, "set"))
            return false;

        var arg = input.Length > 3 ? input[3..].Trim() : "";

        if (arg.Length == 0)
        {
            var all = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Collections.DictionaryEntry kv in Environment.GetEnvironmentVariables())
                all[(string)kv.Key] = (string)kv.Value;
            foreach (var kv in state.Env)
                all[kv.Key] = kv.Value;

            foreach (var kv in all)
                Console.WriteLine($"{kv.Key}={kv.Value}");

            return true;
        }

        int eq = arg.IndexOf('=');
        if (eq < 0)
        {
            _ = RunCmdStreamingAsync("set " + arg, state).GetAwaiter().GetResult();
            return true;
        }

        string name = arg[..eq].Trim();
        string value = arg[(eq + 1)..];

        if (name.Length == 0)
        {
            Console.Error.WriteLine("Invalid syntax.");
            return true;
        }

        state.Env[name] = value;
        return true;
    }

    // ----------------------------
    // cmd /c 実行
    // ----------------------------
    static async Task<int> RunCmdStreamingAsync(string command, ShellState state)
    {
        // cmd に流す “1本のスクリプト” を組み立てる
        var sb = new StringBuilder();

        // 文字コード（毎回 cmd 起動するなら仕方ない）
        sb.Append("chcp 932>nul & ");

        // cwd
        sb.Append("cd /d ");
        sb.Append(CmdQuote(state.Cwd));
        sb.Append(" & ");

        // 環境変数：定番の set "A=B"
        foreach (var kv in state.Env)
        {
            sb.Append("set ");
            sb.Append(CmdSetQuote(kv.Key, kv.Value));
            sb.Append(" & ");
        }

        // ユーザーコマンド本体（cmd互換優先ならそのまま）
        sb.Append(command);

        var script = sb.ToString();

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            WorkingDirectory = state.Cwd,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.GetEncoding(932),
            StandardErrorEncoding = Encoding.GetEncoding(932),
        };

        psi.ArgumentList.Add("/d");
        psi.ArgumentList.Add("/q");
        psi.ArgumentList.Add("/s");
        psi.ArgumentList.Add("/c");
        psi.ArgumentList.Add(script);

        using var p = new Process { StartInfo = psi };
        p.Start();

        Task pumpOut = PumpAsync(p.StandardOutput, Console.Out);
        Task pumpErr = PumpAsync(p.StandardError, Console.Error);

        await Task.WhenAll(p.WaitForExitAsync(), pumpOut, pumpErr);
        return p.ExitCode;
    }

    /// <summary>
    /// cd 用： "..." で包む（" は "" に）
    /// </summary>
    static string CmdQuote(string s)
    {
        s ??= "";
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }

    /// <summary>
    /// set 用： set "NAME=VALUE" 形式（" は "" に）
    /// </summary>
    static string CmdSetQuote(string name, string value)
    {
        name ??= "";
        value ??= "";
        return "\"" + name.Replace("\"", "\"\"") + "=" + value.Replace("\"", "\"\"") + "\"";
    }

    /// <summary>
    /// cmd /c 用： /s と合わせて ""..."" で包む。
    /// 中の " は "" にしておく（cmdの外側引用符崩れ対策）
    /// </summary>
    static string CmdWrapForCmdC(string script)
    {
        script ??= "";
        return "\"\"" + script.Replace("\"", "\"\"") + "\"\"";
    }


    static async Task PumpAsync(StreamReader reader, TextWriter writer)
    {
        char[] buf = new char[4096];
        int n;
        while ((n = await reader.ReadAsync(buf, 0, buf.Length)) > 0)
            await writer.WriteAsync(buf, 0, n);
    }

    static bool StartsWithCommand(string input, string cmd)
    {
        if (!input.StartsWith(cmd, StringComparison.OrdinalIgnoreCase)) return false;
        if (input.Length == cmd.Length) return true;
        return char.IsWhiteSpace(input[cmd.Length]);
    }

}
