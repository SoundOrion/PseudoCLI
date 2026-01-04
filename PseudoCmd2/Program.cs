using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task<int> Main()
    {
        Console.Title = "PseudoCLI";
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        Console.WriteLine($"Microsoft Windows [Version {Environment.OSVersion.Version}]");
        Console.WriteLine("(c) Microsoft Corporation. All rights reserved.");
        Console.WriteLine();

        var defaultCwd = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        Directory.SetCurrentDirectory(defaultCwd);

        var state = new ShellState
        {
            Cwd = defaultCwd
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
        public Dictionary<string, string> Env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    // ----------------------------
    // cd
    // ----------------------------
    static bool TryHandleCd(string input, ShellState state)
    {
        if (!StartsWithCommand(input, "cd"))
            return false;

        // var arg = input.Length > 2 ? input[2..].Trim() : "";
        var arg = input.Length > 2 ? input.Substring(2).Trim() : "";

        if (arg.Length == 0)
        {
            Console.WriteLine(state.Cwd);
            return true;
        }

        if (arg.StartsWith("/d", StringComparison.OrdinalIgnoreCase))
            arg = arg.Substring(2).Trim();

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

        // var arg = input.Length > 3 ? input[3..].Trim() : "";
        var arg = input.Length > 3 ? input.Substring(3).Trim() : "";

        if (arg.Length == 0)
        {
            var all = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry kv in Environment.GetEnvironmentVariables())
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
            RunCmdStreamingAsync("set " + arg, state).GetAwaiter().GetResult();
            return true;
        }

        // string name = arg[..eq].Trim();
        // string value = arg[(eq + 1)..];
        string name = arg.Substring(0, eq).Trim();
        string value = (eq + 1 < arg.Length) ? arg.Substring(eq + 1) : "";

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
        var sb = new StringBuilder();

        sb.Append("chcp 65001>nul & ");

        sb.Append("cd /d ");
        sb.Append(CmdQuote(state.Cwd));
        sb.Append(" & ");

        foreach (var kv in state.Env)
        {
            sb.Append("set ");
            sb.Append(CmdSetQuote(kv.Key, kv.Value));
            sb.Append(" & ");
        }

        sb.Append(command);

        var script = sb.ToString();

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c " + script,
            WorkingDirectory = state.Cwd,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.GetEncoding(932),
            StandardErrorEncoding = Encoding.GetEncoding(932),
        };

        using (var p = new Process { StartInfo = psi, EnableRaisingEvents = true })
        {
            p.Start();

            Task pumpOut = PumpAsync(p.StandardOutput, Console.Out);
            Task pumpErr = PumpAsync(p.StandardError, Console.Error);

            // WaitForExitAsync が無い環境向け
            Task wait = WaitForExitAsyncCompat(p);

            await Task.WhenAll(wait, pumpOut, pumpErr);
            return p.ExitCode;
        }
    }

    /// <summary>
    /// cd 用： "..." で包む（" は "" に）
    /// </summary>
    static string CmdQuote(string s)
    {
        if (s == null) s = "";
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }

    /// <summary>
    /// set 用： set "NAME=VALUE" 形式（" は "" に）
    /// </summary>
    static string CmdSetQuote(string name, string value)
    {
        if (name == null) name = "";
        if (value == null) value = "";
        return "\"" + name.Replace("\"", "\"\"") + "=" + value.Replace("\"", "\"\"") + "\"";
    }

    /// <summary>
    /// cmd /c 用： /s と合わせて ""..."" で包む。
    /// </summary>
    static string CmdWrapForCmdC(string script)
    {
        if (script == null) script = "";
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

    /// <summary>
    /// .NET Framework / 旧ターゲット向けの WaitForExitAsync 互換
    /// </summary>
    static Task WaitForExitAsyncCompat(Process process)
    {
        if (process.HasExited)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<object>();

        EventHandler handler = null;
        handler = (s, e) =>
        {
            process.Exited -= handler;
            tcs.TrySetResult(null);
        };

        process.Exited += handler;

        // 競合対策（イベント登録直後に終了している可能性）
        if (process.HasExited)
        {
            process.Exited -= handler;
            tcs.TrySetResult(null);
        }

        return tcs.Task;
    }
}
