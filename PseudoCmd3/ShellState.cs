using System;
using System.Collections.Generic;
using System.IO;

namespace PseudoCLI
{
    internal sealed class ShellState
    {
        public string Cwd = "";
        public Dictionary<string, string> Env =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // prompt の書式（cmd っぽく最低限）
        public string PromptFormat = "$P$G"; // 例: C:\Users\you>

        public ShellState CreateDefault()
        {
            var defaultCwd = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            Directory.SetCurrentDirectory(defaultCwd);

            Cwd = defaultCwd;
            Env.Clear();
            PromptFormat = "$P$G";

            return this;
        }

        public string RenderPrompt()
        {
            // 超ミニ実装：$P=パス, $G='>'
            // 必要なら $T 時刻とか $B | とか増やせる
            return (PromptFormat ?? "$P$G")
                .Replace("$P", Cwd)
                .Replace("$G", ">");
        }
    }
}
