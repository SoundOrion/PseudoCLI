using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PseudoCLI
{
    internal static class CmdRunner
    {
        public static async Task<int> RunCmdStreamingAsync(string command, ShellState state)
        {
            var sb = new StringBuilder();

            sb.Append("chcp 65001>nul & ");

            sb.Append("cd /d ");
            sb.Append(CmdQuoting.CdQuote(state.Cwd));
            sb.Append(" & ");

            foreach (var kv in state.Env)
            {
                sb.Append("set ");
                sb.Append(CmdQuoting.SetQuote(kv.Key, kv.Value));
                sb.Append(" & ");
            }

            sb.Append(command);

            var script = sb.ToString();

            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/d /q /s /c " + script,
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

                Task pumpOut = ProcessPumping.PumpAsync(p.StandardOutput, Console.Out);
                Task pumpErr = ProcessPumping.PumpAsync(p.StandardError, Console.Error);
                Task wait = ProcessCompat.WaitForExitAsyncCompat(p);

                await Task.WhenAll(wait, pumpOut, pumpErr);
                return p.ExitCode;
            }
        }
    }
}
