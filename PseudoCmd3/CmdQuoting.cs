namespace PseudoCLI
{
    internal static class CmdQuoting
    {
        // cd 用："..."（" は ""）
        public static string CdQuote(string s)
        {
            if (s == null) s = "";
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        // set 用： set "NAME=VALUE"（" は ""）
        public static string SetQuote(string name, string value)
        {
            if (name == null) name = "";
            if (value == null) value = "";
            return "\"" + name.Replace("\"", "\"\"") + "=" + value.Replace("\"", "\"\"") + "\"";
        }

        // cmd /c 用（今回未使用だけど残すならここ）
        public static string WrapForCmdC(string script)
        {
            if (script == null) script = "";
            return "\"\"" + script.Replace("\"", "\"\"") + "\"\"";
        }
    }
}
