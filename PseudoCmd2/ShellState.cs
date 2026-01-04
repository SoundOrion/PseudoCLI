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

        public ShellState CreateDefault()
        {
            var defaultCwd = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            Directory.SetCurrentDirectory(defaultCwd);

            Cwd = defaultCwd;
            Env.Clear();

            return this;
        }
    }
}
